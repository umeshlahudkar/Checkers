using System.Collections;
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

    private GameState gameState = GameState.Waiting;
    private GameModeType gameMode;
    private bool isReadyToLeaveGameplay = false;

    private readonly int maxTurnMissCount = 3;
    private readonly int matchWinCoinReward = 500;

    private string player1DisplayName;
    private string player2DisplayName;

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

    public int CurrentTurn { get { return currentTurn; } }

    public PieceType PieceType { get { return pieceType; } }

    public bool IsReadyToLeaveGameplay
    {
        get { return isReadyToLeaveGameplay; }
        set { isReadyToLeaveGameplay = value; }
    }


    private void Start()
    {
        Invoke("InitializeGame", 0.5f);

        //InitializeGame();
    }

    private void InitializeGame()
    {
        gameMode = gameDataSO.gameMode;
        gameState = GameState.Playing;

        if (gameMode == GameModeType.Multiplayer)
        {
            StartCoroutine(PrepareOnlineMode());
        }
        else
        {
            Gameplay.Player opponentPrefab = (gameMode == GameModeType.VsBot) ? (Gameplay.Player)botPlayerPrefab : humanPlayerPrefab;
            SetupLocalMatch(opponentPrefab);
        }
    }

    // Local matches (VsPlayer/VsBot) run through the same PhotonView/RPC-driven gameplay flow as
    // Multiplayer - they just spawn both sides on this one device (PhotonNetwork.OfflineMode is
    // switched on before this scene loads, see OfflineMatchModeHandlerBase) instead of waiting for a
    // second device to join over the network.
    private void SetupLocalMatch(Gameplay.Player opponentPlayerPrefab)
    {
        PlayerInfo ownInfo = gameDataSO.ownPlayer;
        PlayerInfo opponentInfo = gameDataSO.opponentPlayer;

        SpawnLocalPlayer(1, humanPlayerPrefab, ownInfo.pieceType);
        SpawnLocalPlayer(2, opponentPlayerPrefab, opponentInfo.pieceType);

        player1DisplayName = ownInfo.userName;
        player2DisplayName = opponentInfo.userName;

        ServiceLocator.Get<GamePageManager>().OpenPage(GamePageType.GamePage);
        ServiceLocator.Get<GamePageManager>().GamePage.ShowPlayerInfo(ownInfo.userName, ownInfo.avatar,
                  opponentInfo.userName, opponentInfo.avatar);
        ServiceLocator.Get<GamePageManager>().GamePage.InitTurnIndicators(maxTurnMissCount);

        boardGenerator.GenerateBoard();
        boardGenerator.SetBoardOrientation(!PhotonNetwork.IsMasterClient);
        ServiceLocator.Get<GamePageManager>().GamePage.PositionCardsAroundBoard();

        FinishSetupAndStartFirstTurn();

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
        boardGenerator.GenerateBoard();
        // Blocks are always laid out with row 0-2 = white, row 5-7 = black (Player.cs: actor 1 =
        // black, actor 2 = white), so the non-master (white) client needs its board flipped for
        // its own pieces to render at the bottom, matching its own card's position.
        boardGenerator.SetBoardOrientation(!PhotonNetwork.IsMasterClient);
        ServiceLocator.Get<GamePageManager>().GamePage.PositionCardsAroundBoard();
        PhotonNetwork.Instantiate("Prefab/" + humanPlayerPrefab.name, transform.position, Quaternion.identity);

        PlayerInfo player1 = PhotonNetwork.IsMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
        PlayerInfo player2 = PhotonNetwork.IsMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

        player1DisplayName = player1.userName;
        player2DisplayName = player2.userName;

        ServiceLocator.Get<GamePageManager>().OpenPage(GamePageType.GamePage);
        ServiceLocator.Get<GamePageManager>().GamePage.ShowPlayerInfo(player1.userName, player1.avatar,
            player2.userName, player2.avatar);
        ServiceLocator.Get<GamePageManager>().GamePage.InitTurnIndicators(maxTurnMissCount);

        while(!HasBothPlayerReady())
        {
            yield return null;
        }

        FinishSetupAndStartFirstTurn();

        retryButton.SetActive(false);

        yield return new WaitForSeconds(1f);
    }

    private void FinishSetupAndStartFirstTurn()
    {
        boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);
        ServiceLocator.Get<GamePageManager>().GamePage.InitPiecesLeft(GetRemainingPieceCount(1), GetRemainingPieceCount(2));

        currentTurn = 1;
        if (PhotonNetwork.IsMasterClient)
        {
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn, 0);
        }
    }

    private bool HasBothPlayerReady()
    {
        return players[0] != null && players[1] != null;
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
            int nextTurn = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, missCount);
        }
    }

    public void SwitchTurn()
    {
        // Miss count is cumulative for the whole match - a completed move doesn't clear it, so the
        // outgoing player's count is carried over unchanged here (only a timeout in
        // HandleTurnMissCount ever increments it).
        int nextTurn = currentTurn == 1 ? 2 : 1;
        gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn, players[currentTurn - 1].TurnMissCount);
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn, int outgoingPlayerMissCount)
    {
        players[currentTurn - 1].ResetPlayer();
        players[currentTurn - 1].SetTurnMissCount(outgoingPlayerMissCount);

        currentTurn = nextTurn;
        timer.ResetTimer();
        ServiceLocator.Get<GamePageManager>().GamePage.SetActiveTurn(currentTurn);

        if (players[currentTurn - 1].PhotonView.IsMine && !players[currentTurn - 1].CanPlay())
        {
            int winner = (currentTurn == 1) ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "no legal moves left");
            return;
        }

        timer.StartTimer();
    }

    [PunRPC]
    public void GameOver(int winnerPlayerNumber, string reason)
    {
        SetGameOver();

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

        if (isLocalWin)
        {
            //ServiceLocator.Get<CoinManager>().AddCoin(matchWinCoinReward);
            ServiceLocator.Get<GamePageManager>().ResultPage.ShowVictory(loserName, GetRemainingPieceCount(winnerPlayerNumber), matchWinCoinReward);
            ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.ResultPage);
        }
        else
        {
            ServiceLocator.Get<GamePageManager>().ResultPage.ShowDefeat(winnerName, reason);
            ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.ResultPage);
        }
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

    public void SetGameOver()
    {
        gameState = GameState.Ending;
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
    }

    private void ResetGameManager()
    {
        pieceType = PieceType.None;
        currentTurn = -1;
        gameState = GameState.Waiting;
        IsReadyToLeaveGameplay = false;
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
        ServiceLocator.Get<GameplayController>().ResetGameplay();
        ServiceLocator.Get<GamePageManager>().OpenPage(GamePageType.GamePage);
    }

    public void StartRematch()
    {
        ResetGameplay();
        InitializeGame();
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
