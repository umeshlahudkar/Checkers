using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class GameManager : Service<GameManager>
{
    [SerializeField] private GameDataSO gameDataSO;
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PhotonView gameManagerPhotonView;
    [SerializeField] private TimerController timer;

    [SerializeField] private Gameplay.HumanPlayer humanPlayerPrefab;
    [SerializeField] private Gameplay.BotPlayer botPlayerPrefab;

    [SerializeField] private Gameplay.Player[] players = new Gameplay.Player[2];

    private PieceType pieceType;
    private int currentTurn;
    private int movesWithoutProgress;

    // Bumped by ChangeTurn every time it actually applies a transition. SwitchTurn and
    // HandleTurnMissCount each capture the value they saw at the moment they decided to switch and
    // send it along with their ChangeTurn RPC; ChangeTurn only applies (and bumps this) if that
    // captured value still matches, so whichever of the two calls resolves the same transition
    // first "wins" and the other is a stale no-op instead of silently corrupting turn/miss-count/
    // no-progress state by re-applying its own now-outdated view on top.
    private int turnSequence;

    private GameState gameState = GameState.Waiting;
    private GameModeType gameMode;
    private bool enableTurnTimer;
    private bool isReadyToLeaveGameplay = false;

    private readonly int maxTurnMissCount = 3;

    private const string GameplayReadyPropertyKey = "GameplayReady";

    // Cooldown enforced purely on the offerer's own Offer Draw button after sending an offer - runs
    // to completion unconditionally (turn changes, an early accept/decline, none of it cuts this
    // short - see GamePage.StartDrawOfferCountdown), it's a flat "can't send another offer within
    // 15 seconds of the last one" rate limit, not tied to whether that earlier offer is still
    // meaningfully pending. The offer itself has no separate timeout - a real opponent's
    // DrawOfferPage popup stays open until they explicitly accept or decline (see ReceiveDrawOffer).
    private const float DrawOfferButtonCooldownSeconds = 15f;

    private string player1DisplayName;
    private string player2DisplayName;

    // Match stats shown on the result pages, indexed by playerNumber - 1. Captures/kings are
    // updated from Player.DestroyPieceAt/CrownPieceAt (both PunRPCs, so every client's copy stays
    // in sync the same way board state does) and longestChainCount from Player.ReportChainLength;
    // matchStartTime is stamped once per client off PhotonNetwork.Time, the same synced clock
    // TimerController already relies on, so match duration comes out consistent without an RPC.
    private readonly int[] captureCount = new int[2];
    private readonly int[] kingsCrownedCount = new int[2];
    private readonly int[] longestChainCount = new int[2];
    private double matchStartTime;

    // Board state at the start of every turn, offline modes only (see PushHistorySnapshot) - powers
    // Undo. The last entry is always "now" (the turn in progress, not yet played); Undo trims back
    // to an earlier entry rather than trying to reverse individual moves/captures/promotions.
    private readonly List<BoardSnapshot> historyStack = new();

    // True from the moment OfferDraw/ReceiveDrawOffer raises a mutual-agreement offer until the
    // response arrives (or the turn moves on and silently cancels it - see ChangeTurn). Prevents a
    // second offer from being raised while one is already outstanding, and lets ReceiveDrawResponse
    // reject a stale response referring to an offer that no longer applies.
    private bool drawOfferPending;

    // Occurrence count of every board position (piece layout + side to move) reached so far this
    // match, keyed by ComputeBoardHash - powers the threefold-repetition draw. Cleared whenever a
    // capture or promotion happens (see SwitchTurn), since that permanently changes the board and
    // nothing recorded before it can ever recur.
    private readonly Dictionary<long, int> positionRepetitionCounts = new();

    private MaterialDrawPattern materialDrawPattern = MaterialDrawPattern.None;
    private int materialDrawCounter;

    public GameState GameState
    {
        get { return gameState; }
    }

    public GameModeType GameMode
    {
        get { return gameMode; }
    }

    public BotDifficulty BotDifficulty
    {
        get { return gameDataSO.botDifficulty; }
    }

    public BotAISettingsSO BotAISettings
    {
        get { return gameDataSO.botAISettings; }
    }

    private IRuleSet ruleSet;
    public IRuleSet RuleSet { get { return ruleSet; } }

    // Broadcasts to every client rather than just pausing the mover's own local TimerController
    // instance - every client runs its own independent countdown off the synced PhotonNetwork.Time
    // baseline (see TimerController.StartTimer), and it's specifically the MASTER client's own copy
    // that HandleTurnMissCount's timeout enforcement actually listens to, which is a different
    // physical device from the mover about half the time in real Multiplayer. Pausing only locally
    // would close this race for offline modes (mover and master are always the same device there)
    // but leave it wide open whenever the master isn't also the one moving.
    public void PauseTurnTimer()
    {
        gameManagerPhotonView.RPC(nameof(PauseTurnTimerRPC), RpcTarget.All);
    }

    public void ResumeTurnTimer()
    {
        gameManagerPhotonView.RPC(nameof(ResumeTurnTimerRPC), RpcTarget.All);
    }

    [PunRPC]
    public void PauseTurnTimerRPC(PhotonMessageInfo info = default)
    {
        if (!IsAuthorizedSender(info.Sender)) { return; }
        timer.PauseTimer();
    }

    [PunRPC]
    public void ResumeTurnTimerRPC(PhotonMessageInfo info = default)
    {
        if (!IsAuthorizedSender(info.Sender)) { return; }
        timer.ResumeTimer();
    }

    // True for exactly the two sources every gameplay-mutating RPC in this project is meant to
    // originate from: the player whose turn it currently is (the sole legitimate author of RPCs
    // resulting from their own legal move - DestroyPieceAt/CrownPieceAt/ReportChainLength/
    // PauseTurnTimerRPC/ResumeTurnTimerRPC, and SwitchTurn's own ChangeTurn), or the master client
    // (the sole authority for timeout/miss-driven actions - HandleTurnMissCount's ChangeTurn/
    // GameOver, and Player.FlushIncompleteChain sweeping an abandoned chain on a timed-out player's
    // behalf, whose DestroyPieceAt calls the master sends on that player's own PhotonView). A null
    // sender means this was a direct, non-RPC call rather than something PUN delivered - only
    // reachable from code already running in this same process (e.g. this file's own debug
    // ContextMenu shortcuts), so it's inherently trusted. Anything else is either a bug in this
    // codebase's own call sites or a forged RPC from a modified client, and should be dropped
    // rather than applied - this does not re-validate that the underlying move/action was itself
    // legal, only that whoever sent it was allowed to be the one deciding a turn should change.
    public bool IsAuthorizedSender(Photon.Realtime.Player sender)
    {
        if (sender == null) { return true; }
        if (sender.IsMasterClient) { return true; }

        // currentTurn is -1 outside an active match (see ResetGameManager) - fail closed rather
        // than index out of range if a stray RPC somehow arrives in that window.
        if (currentTurn < 1 || currentTurn > players.Length) { return false; }

        Gameplay.Player currentPlayer = players[currentTurn - 1];
        return currentPlayer != null && currentPlayer.PhotonView.Owner == sender;
    }

    public void ShowFloatingText(string text, Color color)
    {
        ServiceLocator.Get<GamePageManager>().GamePage.ShowFloatingText(text, color);
    }

    public int CurrentTurn { get { return currentTurn; } }

    public PieceType PieceType { get { return pieceType; } }

    public bool IsReadyToLeaveGameplay
    {
        get { return isReadyToLeaveGameplay; }
        set { isReadyToLeaveGameplay = value; }
    }


    private void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        yield return null;
        yield return null;

        gameMode = gameDataSO.gameMode;
        enableTurnTimer = gameDataSO.enableTurnTimer;
        gameState = GameState.Playing;

        ruleSet = gameDataSO.ruleSet;
        ServiceLocator.Get<GameplayController>().InitBoard(ruleSet);
        ServiceLocator.Get<MoveGenerator>().Initialize(ruleSet);

        // Whether to actually wait for a networked opponent has to be decided from
        // PhotonNetwork.OfflineMode, not gameMode: OnlineModeHandler's matchmaking-timeout bot
        // fallback deliberately keeps gameMode as Multiplayer (to disguise the bot as a real
        // opponent) while still running entirely offline on this one device - see
        // OnlineModeHandler.UpdatePreGameCountdown. gameDataSO.opponentIsBot is the analogous
        // mode-independent signal for which prefab the local opponent should use.
        if (!PhotonNetwork.OfflineMode)
        {
            StartCoroutine(PrepareOnlineMode());
        }
        else
        {
            Gameplay.Player opponentPrefab = gameDataSO.opponentIsBot ? (Gameplay.Player)botPlayerPrefab : humanPlayerPrefab;
            StartCoroutine(SetupLocalMatch(opponentPrefab));
        }
    }

    // Local matches (VsPlayer/VsBot) run through the same PhotonView/RPC-driven gameplay flow as
    // Multiplayer - they just spawn both sides on this one device (PhotonNetwork.OfflineMode is
    // switched on before this scene loads, see OfflineMatchModeHandlerBase) instead of waiting for a
    // second device to join over the network.
    private IEnumerator SetupLocalMatch(Gameplay.Player opponentPlayerPrefab)
    {
        PlayerInfo ownInfo = gameDataSO.ownPlayer;
        PlayerInfo opponentInfo = gameDataSO.opponentPlayer;

        SpawnLocalPlayer(1, humanPlayerPrefab, ownInfo.pieceType);
        SpawnLocalPlayer(2, opponentPlayerPrefab, opponentInfo.pieceType);

        player1DisplayName = ownInfo.userName;
        player2DisplayName = opponentInfo.userName;

        GamePageManager gamePageManager = ServiceLocator.Get<GamePageManager>();

        gamePageManager.OpenPage(GamePageType.GamePage);
        gamePageManager.GamePage.ShowPlayerInfo(
            ownInfo.userName, ownInfo.avatar, boardGenerator.GetPieceSprite(ownInfo.pieceType),
            opponentInfo.userName, opponentInfo.avatar, boardGenerator.GetPieceSprite(opponentInfo.pieceType));
        gamePageManager.GamePage.InitTurnIndicators(enableTurnTimer ? maxTurnMissCount : 0);
        gamePageManager.GamePage.SetTimerVisible(enableTurnTimer);
        gamePageManager.GamePage.RefreshOfferDrawButtonVisibility();

        // Defensive reset, not just cosmetic: GamePage's countdown coroutine lives on a
        // MonoBehaviour that persists across a rematch, so a fresh match must not inherit a
        // still-running cooldown left over from a previous one (e.g. the match ended some other
        // way while an offer's cooldown was mid-flight).
        gamePageManager.GamePage.StopDrawOfferCountdown();

        boardGenerator.GenerateBoard(ruleSet);
        boardGenerator.SetBoardOrientation(!PhotonNetwork.IsMasterClient);

        gamePageManager.GamePage.PositionCardsAroundBoard();

        GeneratePiecesAndInitUI();
        yield return StartCoroutine(ServiceLocator.Get<GameplayController>().PlayPiecesAppearAnimation());
        StartFirstTurn();
    }

    // Identity can't be auto-derived from the PhotonView's OwnerActorNr the way real multiplayer
    // does (PhotonNetwork.OfflineMode only ever has a single actor, so both objects would resolve
    // to the same owner), so it's assigned explicitly right after spawning.
    private void SpawnLocalPlayer(int playerNumber, Gameplay.Player prefab, PieceType pieceType)
    {
        GameObject spawned = PhotonNetwork.Instantiate("Prefab/" + prefab.name, transform.position, Quaternion.identity);
        Gameplay.Player player = spawned.GetComponent<Gameplay.Player>();
        player.SetPlayer(playerNumber, pieceType);
        ListPlayer(player);
    }

    private IEnumerator PrepareOnlineMode()
    {
        // Clear any stale flag left over from a previous match in this same room (e.g. a
        // rematch) before starting this round's setup.
        SetLocalPlayerGameplayReady(false);

        boardGenerator.GenerateBoard(ruleSet);
        boardGenerator.SetBoardOrientation(!PhotonNetwork.IsMasterClient);

        GamePageManager gamePageManager = ServiceLocator.Get<GamePageManager>();
        gamePageManager.GamePage.PositionCardsAroundBoard();

        PhotonNetwork.Instantiate("Prefab/" + humanPlayerPrefab.name, transform.position, Quaternion.identity);

        PlayerInfo player1 = PhotonNetwork.IsMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
        PlayerInfo player2 = PhotonNetwork.IsMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

        player1DisplayName = player1.userName;
        player2DisplayName = player2.userName;

        gamePageManager.OpenPage(GamePageType.GamePage);
        gamePageManager.GamePage.ShowPlayerInfo(
            player1.userName, player1.avatar, boardGenerator.GetPieceSprite(player1.pieceType),
            player2.userName, player2.avatar, boardGenerator.GetPieceSprite(player2.pieceType));
        gamePageManager.GamePage.InitTurnIndicators(enableTurnTimer ? maxTurnMissCount : 0);
        gamePageManager.GamePage.SetTimerVisible(enableTurnTimer);
        gamePageManager.GamePage.RefreshOfferDrawButtonVisibility();

        // Defensive reset, not just cosmetic: GamePage's countdown coroutine lives on a
        // MonoBehaviour that persists across a rematch, so a fresh match must not inherit a
        // still-running cooldown left over from a previous one (e.g. the match ended some other
        // way while an offer's cooldown was mid-flight).
        gamePageManager.GamePage.StopDrawOfferCountdown();

        while(!HasBothPlayerReady())
        {
            yield return null;
        }

        GeneratePiecesAndInitUI();

        // GeneratePiecesAndInitUI leaves every piece at full scale, and the wait loop just below
        // (for whichever side reaches it first - typically the master) can render several frames
        // before PlayPiecesAppearAnimation ever runs - keep them invisible until then instead of
        // letting them flash fully visible on the board first. See HidePiecesInstantly.
        ServiceLocator.Get<GameplayController>().HidePiecesInstantly();

        // Tell the other client this side has finished its local setup (board/pieces generated),
        // and wait until it confirms the same, before either side starts the first turn. Without
        // this, whichever client finishes first has no way to know if the other is actually ready -
        // starting the turn immediately (or over RPC) could reach the slower client before its own
        // currentTurn/players[] are valid.
        SetLocalPlayerGameplayReady(true);

        while (!AreBothPlayersGameplayReady())
        {
            yield return null;
        }

        yield return StartCoroutine(ServiceLocator.Get<GameplayController>().PlayPiecesAppearAnimation());

        StartFirstTurn();
    }

    private void GeneratePiecesAndInitUI()
    {
        boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);
        ServiceLocator.Get<GamePageManager>().GamePage.InitPiecesLeft(GetRemainingPieceCount(1), GetRemainingPieceCount(2));
    }

    private void StartFirstTurn()
    {
        currentTurn = DetermineFirstTurnPlayer();
        matchStartTime = PhotonNetwork.Time;
        PushHistorySnapshot();
        RecordPositionForRepetition(currentTurn);
        StartTurn();
    }

    // Most rulesets don't fix an opening color (ruleSet.FirstMoveColor is PieceType.None), so they
    // keep the previous "player 1 always opens" behavior unchanged. A few fix it instead
    // (Italian/Spanish/Canadian: White; Pool Checkers: Black) - for those, whichever seat currently
    // holds that color goes first. Both players' PieceType are already resolved by this point in
    // every mode (SetupLocalMatch/SpawnLocalPlayer offline, Player.Start's OwnerActorNr derivation
    // online), so this needs no extra network round-trip - each client computes the same answer
    // from state it already has locally.
    private int DetermineFirstTurnPlayer()
    {
        if (ruleSet.FirstMoveColor == PieceType.None) { return 1; }
        return players[0].PieceType == ruleSet.FirstMoveColor ? 1 : 2;
    }

    private bool HasBothPlayerReady()
    {
        return players[0] != null && players[1] != null;
    }

    private void SetLocalPlayerGameplayReady(bool isReady)
    {
        PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { GameplayReadyPropertyKey, isReady } });
    }

    private bool AreBothPlayersGameplayReady()
    {
        foreach (Photon.Realtime.Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.CustomProperties.TryGetValue(GameplayReadyPropertyKey, out object isReady) || !(bool)isReady)
            {
                return false;
            }
        }

        return true;
    }

    public void HandleTurnMissCount()
    {
        // Only the master client is allowed to call time on a turn. The active player's own device
        // can be backgrounded (mobile OS suspends its Update loop), so it can't be trusted to police
        // its own clock; the master's device is the one guaranteed to still be ticking. Offline
        // matches are always their own master, so this applies there too without any extra check.
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        // A timeout can land between two hops of a capture chain, when no coroutine is left running
        // to ever reach HandlePieceMovementAndPieceDelete's own end-of-chain sweep - run it here
        // instead, regardless of which branch below fires. A no-op whenever this player wasn't
        // actually mid-chain (the common case).
        players[currentTurn - 1].FlushIncompleteChain();

        int missCount = players[currentTurn - 1].TurnMissCount + 1;

        if (missCount >= maxTurnMissCount)
        {
            int winner = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "out of time");
        }
        else
        {
            // A timed-out turn made no move at all, so it neither advances nor resets the
            // no-progress count - it's carried over unchanged, same as the miss count above. Same
            // reasoning for the material-draw pattern/counter: nothing about the material changed,
            // so both are carried over unchanged too.
            int nextTurn = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, missCount, movesWithoutProgress, true, turnSequence, (int)materialDrawPattern, materialDrawCounter);
        }
    }

    // progressMade is true if the move just played included a capture or a promotion - anything
    // else (a plain shuffle) counts toward the no-progress draw so a repeating back-and-forth can't
    // run forever.
    public void SwitchTurn(bool progressMade)
    {
        movesWithoutProgress = progressMade ? 0 : movesWithoutProgress + 1;

        // 0 disables this rule entirely (Spanish/Canadian/Pool Checkers - see IRuleSet.NoProgressMoveLimit
        // for why that's safe once ThreefoldRepetitionEnabled is guaranteeing termination instead).
        if (ruleSet.NoProgressMoveLimit > 0 && movesWithoutProgress >= ruleSet.NoProgressMoveLimit)
        {
            gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "no progress for too long");
            return;
        }

        // A capture or promotion permanently changes the board, so no position recorded before this
        // point can ever recur - clearing here keeps the repetition table from growing across a
        // whole match and (more importantly) stops an old count from an earlier, now-unreachable
        // material state ever contributing to a draw it has nothing to do with.
        if (progressMade)
        {
            positionRepetitionCounts.Clear();
        }

        int nextTurn = currentTurn == 1 ? 2 : 1;

        if (CheckRepetitionDraw(nextTurn)) { return; }
        if (CheckMaterialDraw()) { return; }

        // Miss count is cumulative for the whole match - a completed move doesn't clear it, so the
        // outgoing player's count is carried over unchanged here (only a timeout in
        // HandleTurnMissCount ever increments it). materialDrawPattern/materialDrawCounter were just
        // freshly recomputed by CheckMaterialDraw above (on this client only, like
        // movesWithoutProgress) - passing them through here is what lets ChangeTurn broadcast that
        // same computation to every other client instead of leaving it stuck at whatever it was last
        // synced to.
        gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, players[currentTurn - 1].TurnMissCount, movesWithoutProgress, false, turnSequence, (int)materialDrawPattern, materialDrawCounter);
    }

    // A cheap combinatorial hash (not cryptographic - collisions are not a realistic concern at this
    // piece/square count) over every occupied square's player and king status, folded together with
    // sideToMove - two positions only hash equal if the layout AND the side to move both match,
    // exactly what the threefold-repetition rule compares.
    private long ComputeBoardHash(int sideToMove)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        unchecked
        {
            long hash = 17;
            for (int i = 0; i < ruleSet.Rows; i++)
            {
                for (int j = 0; j < ruleSet.Columns; j++)
                {
                    Piece piece = gameplayController.pieces[i, j];
                    if (piece == null) { continue; }

                    long cellCode = (i * ruleSet.Columns + j + 1) * 397L + piece.Player_ID * 7 + (piece.IsCrownedKing ? 3 : 1);
                    hash = hash * 31 + cellCode;
                }
            }
            return hash * 31 + sideToMove;
        }
    }

    // Records the position about to be played (sideToMove to act) and returns true if this is its
    // 3rd occurrence this match, firing the existing Draw RPC exactly like the no-progress check
    // above. No-op for Italian, whose published rules have no such condition
    // (ThreefoldRepetitionEnabled false).
    private bool CheckRepetitionDraw(int sideToMove)
    {
        if (!ruleSet.ThreefoldRepetitionEnabled) { return false; }

        long hash = ComputeBoardHash(sideToMove);
        int count = positionRepetitionCounts.TryGetValue(hash, out int existing) ? existing + 1 : 1;
        positionRepetitionCounts[hash] = count;

        if (count < 3) { return false; }

        gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "same position repeated three times");
        return true;
    }

    // Records the very first position of the match (before any move has been played) as occurrence
    // 1, so a threefold repetition that happens to return to the opening position is still caught.
    // Can never fire a draw by itself, since the limit is 3.
    private void RecordPositionForRepetition(int sideToMove)
    {
        CheckRepetitionDraw(sideToMove);
    }

    // International/Canadian's material-specific endgame draws: a move counter gated on exact
    // material composition, reset the instant that composition changes (which any capture always
    // causes). Not general endgame theory - a simplified stand-in in the same spirit as the existing
    // no-progress counter, just tied to specific reduced material instead of ply count alone.
    private bool CheckMaterialDraw()
    {
        if (ruleSet.ThreeVsOneKingDrawLimit <= 0 && ruleSet.TwoVsOneKingDrawLimit <= 0) { return false; }

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        MaterialDrawPattern pattern = ClassifyMaterialPattern(gameplayController.blackPieces, gameplayController.whitePieces)
            ?? ClassifyMaterialPattern(gameplayController.whitePieces, gameplayController.blackPieces)
            ?? MaterialDrawPattern.None;

        if (pattern != materialDrawPattern)
        {
            materialDrawPattern = pattern;
            materialDrawCounter = 0;
        }

        if (pattern == MaterialDrawPattern.None) { return false; }

        materialDrawCounter++;
        int limit = pattern == MaterialDrawPattern.ThreeVsOneKing ? ruleSet.ThreeVsOneKingDrawLimit : ruleSet.TwoVsOneKingDrawLimit;
        if (limit <= 0 || materialDrawCounter < limit) { return false; }

        gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "insufficient material to force a win");
        return true;
    }

    // attackingSide must be exactly 3 Kings (no men) or 2 pieces with at most 1 man; defendingSide
    // must be exactly 1 King. Returns null if neither reduced-material pattern matches.
    private MaterialDrawPattern? ClassifyMaterialPattern(List<Piece> attackingSide, List<Piece> defendingSide)
    {
        if (defendingSide.Count != 1 || !defendingSide[0].IsCrownedKing) { return null; }

        int men = 0;
        for (int i = 0; i < attackingSide.Count; i++)
        {
            if (!attackingSide[i].IsCrownedKing) { men++; }
        }

        if (attackingSide.Count == 3 && men == 0) { return MaterialDrawPattern.ThreeVsOneKing; }
        if (attackingSide.Count == 2 && men <= 1) { return MaterialDrawPattern.TwoVsOneKing; }
        return null;
    }

    // expectedSequence is turnSequence as seen by whichever of SwitchTurn/HandleTurnMissCount fired
    // this - both read the same shared local state (currentTurn, movesWithoutProgress, miss counts)
    // to decide independently that a transition should happen, so if a completed move and an
    // independent timeout both fire for what was really the same transition (see
    // Player.HandlePieceMovementAndPieceDelete's PauseTurnTimer/ResumeTurnTimer for why that window
    // is narrow but not provably zero over a real network), whichever RPC actually arrives first
    // still matches the sequence it captured and applies normally; the second one no longer matches
    // (this method already bumped it) and is ignored instead of re-applying its own now-stale view
    // of missCount/movesWithoutProgress on top of a transition that already happened.
    //
    // syncedMaterialDrawPattern/syncedMaterialDrawCounter carry CheckMaterialDraw's own local state
    // the same way syncedMovesWithoutProgress already carries movesWithoutProgress - both are only
    // ever computed on the client whose move just triggered SwitchTurn (see that method), so without
    // being threaded through here every other client's copy would silently stay stuck at whatever it
    // last was, instead of tracking the same real move count CheckMaterialDraw needs it to.
    [PunRPC]
    public void ChangeTurn(int nextTurn, int outgoingPlayerMissCount, int syncedMovesWithoutProgress, bool wasMissedTurn, int expectedSequence, int syncedMaterialDrawPattern, int syncedMaterialDrawCounter, PhotonMessageInfo info = default)
    {
        if (!IsAuthorizedSender(info.Sender)) { return; }
        if (expectedSequence != turnSequence) { return; }
        turnSequence++;

        // A move has actually gone through - any draw offer still outstanding from before this
        // point no longer refers to the current position, so it's silently dropped rather than left
        // to resolve against a board that's since changed. This deliberately does NOT touch the
        // Offer Draw button's own cooldown (see DrawOfferButtonCooldownSeconds) - that keeps
        // counting down regardless of a turn change in between.
        drawOfferPending = false;

        players[currentTurn - 1].ResetPlayer();
        players[currentTurn - 1].SetTurnMissCount(outgoingPlayerMissCount);

        // RPC-free bookkeeping reset only - see Player.ResetChainState's own comment for why this,
        // not FlushIncompleteChain, is what needs to run identically on every client here: a timeout
        // mid-chain only ever runs FlushIncompleteChain locally on the master, so this is the only
        // point a remote client (in particular the timed-out player's own device, whenever it isn't
        // also the master) ever actually clears its own stale chainCaptureCount/capturedThisChain.
        players[currentTurn - 1].ResetChainState();

        // A completed move already clears/carries the last-move highlight itself (see
        // Player.UpdateGrid) - only a timed-out turn (no move played at all) needs this, since
        // otherwise the previous move's highlight would linger across a turn nobody acted on.
        if (wasMissedTurn)
        {
            ServiceLocator.Get<GameplayController>().ClearLastMoveHighlight();
            ServiceLocator.Get<GameplayController>().ClearHintHighlight();
        }

        currentTurn = nextTurn;
        movesWithoutProgress = syncedMovesWithoutProgress;
        materialDrawPattern = (MaterialDrawPattern)syncedMaterialDrawPattern;
        materialDrawCounter = syncedMaterialDrawCounter;
        PushHistorySnapshot();
        StartTurn();
    }

    // Only offline modes track history - Undo isn't offered in Multiplayer, so there's no point
    // paying the per-turn snapshot cost there.
    private void PushHistorySnapshot()
    {
        if (gameMode == GameModeType.Multiplayer) { return; }

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        int rows = ruleSet.Rows;
        int cols = ruleSet.Columns;

        PieceSnapshot[,] cells = new PieceSnapshot[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                Piece piece = gameplayController.pieces[i, j];
                if (piece == null) { continue; }

                cells[i, j] = new PieceSnapshot
                {
                    present = true,
                    playerID = piece.Player_ID,
                    pieceType = piece.PieceType,
                    isCrownedKing = piece.IsCrownedKing
                };
            }
        }

        historyStack.Add(new BoardSnapshot
        {
            currentTurn = currentTurn,
            movesWithoutProgress = movesWithoutProgress,
            player1MissCount = players[0].TurnMissCount,
            player2MissCount = players[1].TurnMissCount,
            cells = cells
        });
    }

    // Index into historyStack of the snapshot Undo should restore to, or -1 if there isn't one.
    // Always skips the last entry ("now", not yet played), then keeps skipping further back past
    // any entries whose turn belonged to a BotPlayer - so in VsBot, one Undo click rewinds both the
    // bot's reply and the human's own move before it; in VsPlayer (both sides human) it only ever
    // skips the one "now" entry, so it rewinds a single move.
    private int ComputeUndoTargetIndex()
    {
        if (gameMode == GameModeType.Multiplayer) { return -1; }

        int index = historyStack.Count - 2;
        while (index >= 0 && players[historyStack[index].currentTurn - 1] is Gameplay.BotPlayer)
        {
            index--;
        }
        return index;
    }

    // Undo is never meaningful mid-capture-chain, and actively dangerous there: historyStack only
    // gets a new entry once per completed turn (see PushHistorySnapshot), so it has no idea a chain
    // is in progress and would happily rewind to a turn *before* the current one's already-played
    // hops. RestoreSnapshot's RestorePieceLayout then destroys every Piece GameObject on the board
    // (see BoardGenerator.ClearAllPieces) to rebuild from the snapshot - including whatever
    // selectedPiece/capturedThisChain still point to from the abandoned chain, neither of which
    // RestoreSnapshot ever resets. The current player's next click would hit
    // OnHighlightedPieceClick's IsChainInProgress guard and call ContinueAfterKill against those now
    // -destroyed references - a MissingReferenceException, not just a leaked piece like the
    // equivalent Hint gap. Checked here rather than only at the button (GamePage.
    // RefreshHintUndoButtons) for the same reason ShowHint checks itself: the button's interactable
    // state is a UI-layer mitigation, not a guarantee this method itself never runs mid-chain.
    public bool CanUndo()
    {
        if (players[currentTurn - 1].IsChainInProgress) { return false; }
        return ComputeUndoTargetIndex() >= 0;
    }

    public void UndoLastMove()
    {
        if (players[currentTurn - 1].IsChainInProgress) { return; }

        int targetIndex = ComputeUndoTargetIndex();
        if (targetIndex < 0) { return; }

        historyStack.RemoveRange(targetIndex + 1, historyStack.Count - targetIndex - 1);
        RestoreSnapshot(historyStack[targetIndex]);
        StartTurn();
    }

    private void RestoreSnapshot(BoardSnapshot snapshot)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        gameplayController.ClearLastMoveHighlight();
        gameplayController.ClearHintHighlight();

        players[0].ResetPlayer();
        players[1].ResetPlayer();

        boardGenerator.RestorePieceLayout(snapshot.cells);

        currentTurn = snapshot.currentTurn;
        movesWithoutProgress = snapshot.movesWithoutProgress;
        players[0].SetTurnMissCount(snapshot.player1MissCount);
        players[1].SetTurnMissCount(snapshot.player2MissCount);

        ServiceLocator.Get<GamePageManager>().GamePage.UpdatePiecesLeft(GetRemainingPieceCount(1), GetRemainingPieceCount(2));
    }

    private void StartTurn()
    {
        timer.ResetTimer();
        ServiceLocator.Get<GamePageManager>().GamePage.SetActiveTurn(currentTurn);

        if (players[currentTurn - 1].PhotonView.IsMine)
        {
            if (TryEndGameOnSingleManVsKing())
            {
                return;
            }

            if (TryEndGameOnOneVsOneDraw())
            {
                return;
            }

            if (!players[currentTurn - 1].CanPlay())
            {
                int winner = (currentTurn == 1) ? 2 : 1;
                gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "no legal moves left");
                return;
            }
        }

        // Turn timer / miss-count enforcement is opt-in per mode (see ModeInfo.enableTurnTimer,
        // carried in via GameDataSO) rather than tied to Multiplayer specifically - a mode with it
        // off never starts the timer, so it simply never runs and HandleTurnMissCount (which only
        // fires from its countdown reaching zero) never triggers either.
        if (enableTurnTimer)
        {
            timer.StartTimer();
        }
    }

    // Turkish dama's single-man-vs-Dama instant-win rule: a pure board-state check independent of
    // whose turn it is, so it's checked once per turn transition (a capture is the only way piece
    // counts change) rather than tied to currentTurn specifically.
    //
    // gameplayController.blackPieces/whitePieces are, despite their names, partitioned by player
    // number (playerID==1/2), not by each piece's actually-displayed color - see
    // BoardGenerator.SpawnPiece/Piece.Destroy, which both bucket purely on playerID regardless of
    // the PieceType passed in. Pairing blackPieces with player 1 and whitePieces with player 2
    // below is therefore always correct, even in offline modes where player 1's real displayed
    // color is randomized (see PvcModeHandler/PvpModeHandler) - this rule only cares which player
    // is reduced to one man, never which color they happen to be rendered as.
    private bool TryEndGameOnSingleManVsKing()
    {
        if (!ruleSet.SingleManLosesToKing) { return false; }

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        int winner = GetSingleManVsKingWinner(gameplayController.blackPieces, gameplayController.whitePieces, 2);
        if (winner == 0)
        {
            winner = GetSingleManVsKingWinner(gameplayController.whitePieces, gameplayController.blackPieces, 1);
        }

        if (winner == 0) { return false; }

        gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "reduced to a single man against a Dama");
        return true;
    }

    // Turkish dama's "1 vs 1" rule: both sides reduced to exactly one piece each is an automatic
    // draw. Checked only after TryEndGameOnSingleManVsKing has already had first refusal, above - a
    // lone man against a Dama must still win outright via that rule rather than draw here, so this
    // only ever actually fires for a King-vs-King (or man-vs-man) ending.
    private bool TryEndGameOnOneVsOneDraw()
    {
        if (!ruleSet.OneVsOneIsDraw) { return false; }

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        if (gameplayController.blackPieces.Count != 1 || gameplayController.whitePieces.Count != 1) { return false; }

        gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "one piece against one piece");
        return true;
    }

    // If reducedSidePieces has been reduced to exactly one non-king piece and otherSidePieces has
    // at least one King, otherSidePlayerNumber instantly wins. Returns 0 if the condition doesn't
    // hold.
    private int GetSingleManVsKingWinner(List<Piece> reducedSidePieces, List<Piece> otherSidePieces, int otherSidePlayerNumber)
    {
        if (reducedSidePieces.Count != 1 || reducedSidePieces[0].IsCrownedKing) { return 0; }

        for (int i = 0; i < otherSidePieces.Count; i++)
        {
            if (otherSidePieces[i].IsCrownedKing) { return otherSidePlayerNumber; }
        }
        return 0;
    }

    // Either player may raise a draw offer at any time, not just on their own turn - identified by
    // GetLocalPlayerNumber() (which seat's PhotonView the calling client actually owns), not by
    // currentTurn. Deliberately does NOT reuse IsAuthorizedSender here: that check's master-client
    // bypass exists so the master can act on a *timed-out* player's behalf elsewhere in this file -
    // there's no equivalent legitimate case for a draw offer, and reusing it here would let the
    // master client raise an offer while claiming to be whoever currentTurn happens to name, then
    // "accept" its own offer as the other seat and force a draw with no real consent from anyone.
    // Only reachable when gameMode is Multiplayer - GamePage hides the Offer Draw button entirely
    // for VsBot/VsPlayer (see GamePage.RefreshOfferDrawButtonVisibility). Note that "Multiplayer"
    // here doesn't necessarily mean networked: a disguised bot-fallback match reports Multiplayer
    // too (see OnlineModeHandler.OnMatchmakingTimeout) - see ReceiveDrawOffer for how that case
    // still resolves correctly.
    public void OfferDraw()
    {
        if (gameState != GameState.Playing || drawOfferPending) { return; }

        drawOfferPending = true;
        gameManagerPhotonView.RPC(nameof(ReceiveDrawOffer), RpcTarget.All, GetLocalPlayerNumber());
    }

    [PunRPC]
    public void ReceiveDrawOffer(int offeringPlayerNumber, PhotonMessageInfo info = default)
    {
        if (gameState != GameState.Playing || !IsOwnedBy(offeringPlayerNumber, info.Sender)) { return; }
        drawOfferPending = true;

        // Sent to RpcTarget.All rather than Others so the same call works in every mode - in
        // Multiplayer the offerer's own client also receives this and should just be told the offer
        // went out, not shown a prompt to respond to itself. Offline VsPlayer (pass-and-play) has no
        // such distinction - both seats share one device, so it always shows the prompt, same
        // convention Player.UpdateGrid already uses for VsPlayer's move highlighting.
        if (gameMode == GameModeType.Multiplayer && players[offeringPlayerNumber - 1].PhotonView.IsMine)
        {
            ShowFloatingText("Draw offer sent", Color.white);
            ServiceLocator.Get<GamePageManager>().GamePage.StartDrawOfferCountdown(DrawOfferButtonCooldownSeconds);

            // A disguised bot-fallback match (see OnlineModeHandler.OnMatchmakingTimeout) reports
            // gameMode as Multiplayer but runs entirely on this one device in
            // PhotonNetwork.OfflineMode - both seats are locally owned, so the check above is
            // always true and there's no second real client left to ever receive this same RPC and
            // respond for real. The bot has to simulate accepting it here instead, through the same
            // RespondToDrawOffer path and wording a real opponent's acceptance would use.
            if (PhotonNetwork.OfflineMode && gameDataSO.opponentIsBot)
            {
                StartCoroutine(AutoAcceptDrawOffer(offeringPlayerNumber));
            }

            return;
        }

        // A real opponent's popup has no dismiss/close option - it stays open (blocking, via
        // OpenPageAsOverlay) until they explicitly accept or decline via DrawOfferPage's own two
        // buttons. There's deliberately no timeout here to auto-close it.
        GamePageManager gamePageManager = ServiceLocator.Get<GamePageManager>();
        gamePageManager.DrawOfferPage.Show(offeringPlayerNumber);
        gamePageManager.OpenPageAsOverlay(GamePageType.DrawOfferPage);
    }

    // A material lead of this many points (man = 1, king = 2) or more is what makes the bot decline
    // instead of accepting - deliberately a simple material-only read of the board, not a call into
    // the standalone Minimax/BotMinimax evaluator, since a draw decision only needs "am I clearly
    // ahead", not a full move search.
    private const int BotDrawDeclineMaterialLead = 2;

    // The bot's "thinking" delay before responding - see GameManager.OfferDraw's comment and
    // OnlineModeHandler.OnMatchmakingTimeout for why this has to look identical to a real opponent.
    // The response itself now actually weighs the board (see ShouldBotAcceptDraw) rather than
    // always accepting.
    private IEnumerator AutoAcceptDrawOffer(int offeringPlayerNumber)
    {
        yield return new WaitForSeconds(Random.Range(3f, 5f));
        RespondToDrawOffer(offeringPlayerNumber, ShouldBotAcceptDraw(offeringPlayerNumber));
    }

    // The bot only turns a draw down when it's clearly ahead on material - anything level, close,
    // or where the bot is actually behind still gets accepted, so it reads as a reasonable opponent
    // rather than one that never settles for a draw it could still win from.
    private bool ShouldBotAcceptDraw(int offeringPlayerNumber)
    {
        int botPlayerNumber = offeringPlayerNumber == 1 ? 2 : 1;
        int botScore = ComputeMaterialScore(botPlayerNumber);
        int offererScore = ComputeMaterialScore(offeringPlayerNumber);

        return botScore - offererScore < BotDrawDeclineMaterialLead;
    }

    // whitePieces/blackPieces are player-number buckets, not color buckets - see
    // GetRemainingPieceCount's own comment for why player 2 pairs with whitePieces.
    private int ComputeMaterialScore(int playerNumber)
    {
        List<Piece> pieces = playerNumber == 2
            ? ServiceLocator.Get<GameplayController>().whitePieces
            : ServiceLocator.Get<GameplayController>().blackPieces;

        int score = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            score += pieces[i].IsCrownedKing ? 2 : 1;
        }
        return score;
    }

    public void RespondToDrawOffer(int offeringPlayerNumber, bool accepted)
    {
        gameManagerPhotonView.RPC(nameof(ReceiveDrawResponse), RpcTarget.All, offeringPlayerNumber, accepted);
    }

    [PunRPC]
    public void ReceiveDrawResponse(int offeringPlayerNumber, bool accepted, PhotonMessageInfo info = default)
    {
        if (!drawOfferPending || !IsOwnedBy(offeringPlayerNumber == 1 ? 2 : 1, info.Sender)) { return; }
        drawOfferPending = false;

        if (accepted)
        {
            gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "mutual agreement");
        }
        else
        {
            ShowFloatingText("Draw declined", Color.white);
        }
    }

    // sender must actually own playerNumber's seat, or be null (a direct, non-RPC call - only
    // reachable from code already running in this same process, same convention as
    // IsAuthorizedSender's own null-sender case). Deliberately NO master-client bypass, unlike
    // IsAuthorizedSender - there is no legitimate case here where the master needs to act as, or on
    // behalf of, a seat it doesn't own; a bypass would let it impersonate either side of a draw
    // negotiation. In offline modes both seats' PhotonViews share the same single local owner, so
    // this passes for either playerNumber there without needing a special case.
    private bool IsOwnedBy(int playerNumber, Photon.Realtime.Player sender)
    {
        if (sender == null) { return true; }
        return players[playerNumber - 1] != null && players[playerNumber - 1].PhotonView.Owner == sender;
    }

    // info.Sender is auto-supplied by PUN when this arrives as a real RPC; the default lets the
    // debug ContextMenu shortcuts below call this directly with no sender at all, which
    // IsAuthorizedSender treats as inherently trusted since only code already running in this same
    // process can reach a direct (non-RPC) call in the first place.
    [PunRPC]
    public void GameOver(int winnerPlayerNumber, string reason, PhotonMessageInfo info = default)
    {
        if (!IsAuthorizedSender(info.Sender)) { return; }

        // Guards against two near-simultaneous end-of-match RPCs (e.g. a no-progress Draw and an
        // out-of-time GameOver racing each other - see H5/H6) both applying and showing two
        // stacked/contradictory result screens. Safe to check synchronously here: PrepareGameOverVisuals
        // (reached via PlayGameOverSequence below) calls SetGameOver() as its very first action,
        // before any yield, so gameState already reflects a first call that's merely still mid-coroutine.
        if (gameState != GameState.Playing) { return; }

        StartCoroutine(PlayGameOverSequence(winnerPlayerNumber, reason));
    }

    private IEnumerator PlayGameOverSequence(int winnerPlayerNumber, string reason)
    {
        yield return StartCoroutine(PrepareGameOverVisuals());

        // Same PhotonNetwork.OfflineMode-based distinction as InitializeGame, not gameMode - a
        // disguised bot-fallback match reports Multiplayer but runs fully offline, where every
        // PhotonView (both seats) is locally owned, so PhotonView.IsMine would always be true and
        // resolve every win as a local win regardless of who actually won.
        bool isLocalWin;
        if (!PhotonNetwork.OfflineMode)
        {
            isLocalWin = players[winnerPlayerNumber - 1].PhotonView.IsMine;
        }
        else
        {
            isLocalWin = winnerPlayerNumber == 1;
        }

        int loserPlayerNumber = winnerPlayerNumber == 1 ? 2 : 1;
        string winnerName = winnerPlayerNumber == 1 ? player1DisplayName : player2DisplayName;
        string loserName = loserPlayerNumber == 1 ? player1DisplayName : player2DisplayName;

        int localPlayerNumber = isLocalWin ? winnerPlayerNumber : loserPlayerNumber;
        int opponentPlayerNumber = isLocalWin ? loserPlayerNumber : winnerPlayerNumber;

        //if (isLocalWin) ServiceLocator.Get<CoinManager>().AddCoin(matchWinCoinReward);
        GameResult result = new GameResult
        {
            Outcome = isLocalWin ? GameOutcome.Victory : GameOutcome.Defeat,
            OpponentName = isLocalWin ? loserName : winnerName,
            OpponentAvatar = gameDataSO.opponentPlayer.avatar,
            LocalPiecesLeft = GetRemainingPieceCount(localPlayerNumber),
            OpponentPiecesLeft = GetRemainingPieceCount(opponentPlayerNumber),
            Reason = reason,
            LocalCaptures = captureCount[localPlayerNumber - 1],
            OpponentCaptures = captureCount[opponentPlayerNumber - 1],
            LocalKingsCrowned = kingsCrownedCount[localPlayerNumber - 1],
            OpponentKingsCrowned = kingsCrownedCount[opponentPlayerNumber - 1],
            LocalLongestChain = longestChainCount[localPlayerNumber - 1],
            OpponentLongestChain = longestChainCount[opponentPlayerNumber - 1],
            MatchDuration = GetMatchDurationText()
        };

        ServiceLocator.Get<AudioManager>().PlayGameOverSound();
        if (isLocalWin)
        {
            ServiceLocator.Get<AudioManager>().PlayGameWinSound();
        }
        else
        {
            ServiceLocator.Get<AudioManager>().PlayGameLoseSound();
        }

        ServiceLocator.Get<GamePageManager>().ShowGameResult(result);
    }

    [PunRPC]
    public void Draw(string reason, PhotonMessageInfo info = default)
    {
        if (!IsAuthorizedSender(info.Sender)) { return; }

        // Same already-ended guard as GameOver above - see there for why the synchronous check is
        // safe even against a first call that's still mid-coroutine.
        if (gameState != GameState.Playing) { return; }

        StartCoroutine(PlayDrawSequence(reason));
    }

    private IEnumerator PlayDrawSequence(string reason)
    {
        yield return StartCoroutine(PrepareGameOverVisuals());

        int localPlayerNumber = GetLocalPlayerNumber();
        int opponentPlayerNumber = localPlayerNumber == 1 ? 2 : 1;

        GameResult result = new GameResult
        {
            Outcome = GameOutcome.Draw,
            OpponentName = gameDataSO.opponentPlayer.userName,
            OpponentAvatar = gameDataSO.opponentPlayer.avatar,
            LocalPiecesLeft = GetRemainingPieceCount(localPlayerNumber),
            OpponentPiecesLeft = GetRemainingPieceCount(opponentPlayerNumber),
            Reason = reason,
            LocalCaptures = captureCount[localPlayerNumber - 1],
            OpponentCaptures = captureCount[opponentPlayerNumber - 1],
            LocalKingsCrowned = kingsCrownedCount[localPlayerNumber - 1],
            OpponentKingsCrowned = kingsCrownedCount[opponentPlayerNumber - 1],
            LocalLongestChain = longestChainCount[localPlayerNumber - 1],
            OpponentLongestChain = longestChainCount[opponentPlayerNumber - 1],
            MatchDuration = GetMatchDurationText()
        };

        ServiceLocator.Get<AudioManager>().PlayGameOverSound();
        ServiceLocator.Get<AudioManager>().PlayGameDrawSound();

        ServiceLocator.Get<GamePageManager>().ShowGameResult(result);
    }

    // Multiplayer's player1/player2 slots flip with master-client role (see PrepareOnlineMode), so
    // "local" has to be resolved via PhotonView.IsMine there; offline modes always seat the local
    // player as player 1 (see SetupLocalMatch), matching the convention already used by isLocalWin
    // above.
    private int GetLocalPlayerNumber()
    {
        if (gameMode == GameModeType.Multiplayer)
        {
            return players[0].PhotonView.IsMine ? 1 : 2;
        }
        return 1;
    }

    public void RegisterCapture(int playerNumber)
    {
        captureCount[playerNumber - 1]++;
    }

    public void RegisterKingCrowned(int playerNumber)
    {
        kingsCrownedCount[playerNumber - 1]++;
    }

    public void RegisterChainLength(int playerNumber, int chainLength)
    {
        if (chainLength > longestChainCount[playerNumber - 1])
        {
            longestChainCount[playerNumber - 1] = chainLength;
        }
    }

    private string GetMatchDurationText()
    {
        int totalSeconds = Mathf.Max(0, (int)(PhotonNetwork.Time - matchStartTime));
        return $"{totalSeconds / 60}:{totalSeconds % 60:D2}";
    }

    // Called when the opponent leaves the match (see MatchSessionEventManager.PlayForfeitSequence) -
    // always a local win since the only way to forfeit is for the *other* side to leave.
    //
    // Deliberately has no gameState guard of its own, unlike GameOver/Draw above: its caller
    // (MatchSessionEventManager.PlayForfeitSequence) already runs PrepareGameOverVisuals - which
    // sets gameState to Ending as its first synchronous action - immediately before calling this,
    // every time, including on a legitimate first call. A guard here would see its own caller's
    // transition and always reject, never actually running. The equivalent protection for THIS
    // path lives one level up, in MatchSessionEventManager.OnPlayerLeftRoom, which checks GameState
    // before ever starting that sequence in the first place - see there.
    public void ShowVictoryByForfeit()
    {
        int localPlayerNumber = GetLocalPlayerNumber();
        int opponentPlayerNumber = localPlayerNumber == 1 ? 2 : 1;

        GameResult result = new GameResult
        {
            Outcome = GameOutcome.Victory,
            OpponentName = gameDataSO.opponentPlayer.userName,
            OpponentAvatar = gameDataSO.opponentPlayer.avatar,
            LocalPiecesLeft = GetRemainingPieceCount(localPlayerNumber),
            OpponentPiecesLeft = GetRemainingPieceCount(opponentPlayerNumber),
            Reason = "Your opponent left the match",
            LocalCaptures = captureCount[localPlayerNumber - 1],
            OpponentCaptures = captureCount[opponentPlayerNumber - 1],
            LocalKingsCrowned = kingsCrownedCount[localPlayerNumber - 1],
            OpponentKingsCrowned = kingsCrownedCount[opponentPlayerNumber - 1],
            LocalLongestChain = longestChainCount[localPlayerNumber - 1],
            OpponentLongestChain = longestChainCount[opponentPlayerNumber - 1],
            MatchDuration = GetMatchDurationText()
        };
        ServiceLocator.Get<GamePageManager>().ShowGameResult(result);
    }

    // Shared by every end-of-match path (a decisive winner, a no-progress draw, or the opponent
    // forfeiting by leaving) - mark the match over, clear any leftover highlight (e.g. a timed-out
    // player's turn-start highlighting was never dismissed by a click), then let all remaining
    // pieces play their disappear animation before the caller shows the result screen.
    public IEnumerator PrepareGameOverVisuals()
    {
        SetGameOver();

        // Stops the active card's low-time blink (a Draw or a forfeit can land while it's mid-blink -
        // ChangeTurn's own ResetTimer() call never runs on those paths since there's no next turn).
        timer.ResetTimer();

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null)
            {
                players[i].ResetPlayer();
            }
        }

        yield return StartCoroutine(ServiceLocator.Get<GameplayController>().PlayPiecesDisappearAnimation());
    }

    // whitePieces/blackPieces are player-number buckets (see TryEndGameOnSingleManVsKing above),
    // not color buckets, so this pairing is correct regardless of either player's displayed color.
    private int GetRemainingPieceCount(int playerNumber)
    {
        return playerNumber == 2
            ? ServiceLocator.Get<GameplayController>().whitePieces.Count
            : ServiceLocator.Get<GameplayController>().blackPieces.Count;
    }

    [ContextMenu("Force Win")]
    private void ForceWin()
    {
        GameOver(1, "debug win");
    }

    [ContextMenu("Force Lose")]
    private void ForceLose()
    {
        GameOver(2, "debug loss");
    }

    [ContextMenu("Force Draw")]
    private void ForceDraw()
    {
        Draw("debug draw");
    }

    [ContextMenu("Test Floating Text")]
    private void TestFloatingText()
    {
        ShowFloatingText("DOUBLE KILL!", new Color(1f, 0.3f, 0.3f));
    }

    public void SetGameOver()
    {
        gameState = GameState.Ending;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
    }

    private void ResetGameManager()
    {
        pieceType = PieceType.None;
        currentTurn = -1;
        movesWithoutProgress = 0;
        turnSequence = 0;
        gameState = GameState.Waiting;
        IsReadyToLeaveGameplay = false;
        drawOfferPending = false;
        positionRepetitionCounts.Clear();
        materialDrawPattern = MaterialDrawPattern.None;
        materialDrawCounter = 0;

        for (int i = 0; i < 2; i++)
        {
            captureCount[i] = 0;
            kingsCrownedCount[i] = 0;
            longestChainCount[i] = 0;
        }
    }

    public Gameplay.Player GetPlayer(int playerID)
    {
        if(playerID > 0 && playerID <= players.Length)
        {
            return players[playerID - 1];
        }
        return null;
    }

    public void ListPlayer(Gameplay.Player player)
    {
        players[player.Player_ID - 1] = player;
    }

    private void ResetGameplay()
    {
        // Every mode now spawns players via PhotonNetwork.Instantiate (see SpawnLocalPlayer), so a
        // rematch has to release those network objects here regardless of mode, not just online.
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }
        ResetGameManager();
        historyStack.Clear();
        ServiceLocator.Get<GameplayController>().ResetGameplay();
        ServiceLocator.Get<GamePageManager>().OpenPage(GamePageType.GamePage);
    }

    public void StartRematch()
    {
        ResetGameplay();
        StartCoroutine(InitializeGame());
        //StartCoroutine(Rematch());
    }

    private IEnumerator Rematch()
    {
        ResetGameplay();
        yield return new WaitForSeconds(2f);
        InitializeGame();
    }

    public void OnQuitConfirmed()
    {
        if (gameMode == GameModeType.Multiplayer && PhotonNetwork.IsConnected)
        {
            IsReadyToLeaveGameplay = true;

            // Mark the match as ending before disconnecting - Disconnect() (unlike LeaveRoom())
            // fires OnDisconnected, and MatchSessionEventManager.OnDisconnected would otherwise
            // kick off its own redundant LoadMainMenu() alongside the one started below.
            SetGameOver();

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            // Disconnect entirely rather than just leaving the room - once the player is back at
            // the menu there's no reason to keep holding a Photon connection open (idle CCU) until
            // they actively choose to matchmake again.
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.Disconnect();
            ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
            ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

            StartCoroutine(LoadMainMenu());
        }
        else
        {
            ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
            StartCoroutine(LoadMainMenu());
        }
    }

    public IEnumerator LoadMainMenu()
    {
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(0);
    }
}

public enum GameState
{
    Waiting,
    Playing,
    Ending
}

public enum MaterialDrawPattern
{
    None,
    ThreeVsOneKing,
    TwoVsOneKing
}

// A single board cell's contents at the moment a BoardSnapshot was taken - default value (all
// fields false/0/None) correctly represents an empty cell.
public struct PieceSnapshot
{
    public bool present;
    public int playerID;
    public PieceType pieceType;
    public bool isCrownedKing;
}

public class BoardSnapshot
{
    public int currentTurn;
    public int movesWithoutProgress;
    public int player1MissCount;
    public int player2MissCount;
    public PieceSnapshot[,] cells;
}
