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
    [SerializeField] private GameObject retryButton;

    [SerializeField] private Gameplay.HumanPlayer humanPlayerPrefab;
    [SerializeField] private Gameplay.BotPlayer botPlayerPrefab;

    [SerializeField] private Gameplay.Player[] players = new Gameplay.Player[2];

    private PieceType pieceType;
    private int currentTurn;
    private int movesWithoutProgress;

    private GameState gameState = GameState.Waiting;
    private GameModeType gameMode;
    private bool isReadyToLeaveGameplay = false;

    private readonly int maxTurnMissCount = 3;

    private const string GameplayReadyPropertyKey = "GameplayReady";

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
        gameState = GameState.Playing;

        ruleSet = gameDataSO.ruleSet;
        ServiceLocator.Get<GameplayController>().InitBoard(ruleSet);
        ServiceLocator.Get<MoveGenerator>().Initialize(ruleSet);

        if (gameMode == GameModeType.Multiplayer)
        {
            StartCoroutine(PrepareOnlineMode());
        }
        else
        {
            Gameplay.Player opponentPrefab = (gameMode == GameModeType.VsBot) ? (Gameplay.Player)botPlayerPrefab : humanPlayerPrefab;
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
        gamePageManager.GamePage.InitTurnIndicators(0);
        gamePageManager.GamePage.SetTimerVisible(false);

        boardGenerator.GenerateBoard(ruleSet);
        boardGenerator.SetBoardOrientation(!PhotonNetwork.IsMasterClient);

        gamePageManager.GamePage.PositionCardsAroundBoard();

        GeneratePiecesAndInitUI();
        yield return StartCoroutine(ServiceLocator.Get<GameplayController>().PlayPiecesAppearAnimation());
        StartFirstTurn();

        retryButton.SetActive(true);
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
        gamePageManager.GamePage.InitTurnIndicators(maxTurnMissCount);
        gamePageManager.GamePage.SetTimerVisible(true);

        while(!HasBothPlayerReady())
        {
            yield return null;
        }

        GeneratePiecesAndInitUI();

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
        retryButton.SetActive(false);
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

        int missCount = players[currentTurn - 1].TurnMissCount + 1;

        if (missCount >= maxTurnMissCount)
        {
            int winner = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "out of time");
        }
        else
        {
            // A timed-out turn made no move at all, so it neither advances nor resets the
            // no-progress count - it's carried over unchanged, same as the miss count above.
            int nextTurn = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, missCount, movesWithoutProgress, true);
        }
    }

    // progressMade is true if the move just played included a capture or a promotion - anything
    // else (a plain shuffle) counts toward the no-progress draw so a repeating back-and-forth can't
    // run forever.
    public void SwitchTurn(bool progressMade)
    {
        movesWithoutProgress = progressMade ? 0 : movesWithoutProgress + 1;

        if (movesWithoutProgress >= ruleSet.NoProgressMoveLimit)
        {
            gameManagerPhotonView.RPC(nameof(Draw), RpcTarget.All, "no progress for too long");
            return;
        }

        // Miss count is cumulative for the whole match - a completed move doesn't clear it, so the
        // outgoing player's count is carried over unchanged here (only a timeout in
        // HandleTurnMissCount ever increments it).
        int nextTurn = currentTurn == 1 ? 2 : 1;
        gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, players[currentTurn - 1].TurnMissCount, movesWithoutProgress, false);
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn, int outgoingPlayerMissCount, int syncedMovesWithoutProgress, bool wasMissedTurn)
    {
        players[currentTurn - 1].ResetPlayer();
        players[currentTurn - 1].SetTurnMissCount(outgoingPlayerMissCount);

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

    public bool CanUndo()
    {
        return ComputeUndoTargetIndex() >= 0;
    }

    public void UndoLastMove()
    {
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

            if (!players[currentTurn - 1].CanPlay())
            {
                int winner = (currentTurn == 1) ? 2 : 1;
                gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "no legal moves left");
                return;
            }
        }

        // Turn timer / miss-count enforcement is a Multiplayer-only concern - offline matches
        // (VsBot/VsPlayer) let a player take as long as they like, so the timer simply never runs
        // there and HandleTurnMissCount (which only fires from its countdown reaching zero) never
        // triggers either.
        if (gameMode == GameModeType.Multiplayer)
        {
            timer.StartTimer();
        }
    }

    // Turkish dama's single-man-vs-Dama instant-win rule: a pure board-state check independent of
    // whose turn it is, so it's checked once per turn transition (a capture is the only way piece
    // counts change) rather than tied to currentTurn specifically.
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

    [PunRPC]
    public void GameOver(int winnerPlayerNumber, string reason)
    {
        StartCoroutine(PlayGameOverSequence(winnerPlayerNumber, reason));
    }

    private IEnumerator PlayGameOverSequence(int winnerPlayerNumber, string reason)
    {
        yield return StartCoroutine(PrepareGameOverVisuals());

        bool isLocalWin;
        if (gameMode == GameModeType.Multiplayer)
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
        ServiceLocator.Get<GamePageManager>().ShowGameResult(result);
    }

    [PunRPC]
    public void Draw(string reason)
    {
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
        gameState = GameState.Waiting;
        IsReadyToLeaveGameplay = false;

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
