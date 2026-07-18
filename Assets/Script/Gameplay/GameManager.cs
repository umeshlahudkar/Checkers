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
        InitializeGame();
    }

    private void InitializeGame()
    {
        gameMode = gameDataSO.gameMode;
        gameState = GameState.Playing;

        if (gameMode == GameModeType.Multiplayer)
        {
            StartCoroutine(PrepareOnlineMode());
        }
        else if (gameMode == GameModeType.VsPlayer)
        {
            SetupLocalMatch(humanPlayerPrefab, humanPlayerPrefab);
        }
        else
        {
            SetupLocalMatch(humanPlayerPrefab, botPlayerPrefab);
        }
    }

    private void SetupLocalMatch(Gameplay.Player ownPlayerPrefab, Gameplay.Player opponentPlayerPrefab)
    {
        PlayerInfo ownInfo = gameDataSO.ownPlayer;
        PlayerInfo opponentInfo = gameDataSO.opponentPlayer;

        players[0] = Instantiate(ownPlayerPrefab, transform.position, Quaternion.identity);
        players[0].SetPlayer(1, ownInfo.pieceType);

        players[1] = Instantiate(opponentPlayerPrefab, transform.position, Quaternion.identity);
        players[1].SetPlayer(2, opponentInfo.pieceType);

        player1DisplayName = ownInfo.userName;
        player2DisplayName = opponentInfo.userName;

        ServiceLocator.Get<GamePageManager>().OpenPage(GamePageType.GamePage);
        ServiceLocator.Get<GamePageManager>().GamePage.ShowPlayerInfo(ownInfo.userName, ownInfo.avatar,
                  opponentInfo.userName, opponentInfo.avatar);
        ServiceLocator.Get<GamePageManager>().GamePage.InitTurnIndicators(maxTurnMissCount);

        boardGenerator.GenerateBoard();
        boardGenerator.SetBoardOrientation(false);
        ServiceLocator.Get<GamePageManager>().GamePage.PositionCardsAroundBoard();
        boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);
        ServiceLocator.Get<GamePageManager>().GamePage.InitPiecesLeft(GetRemainingPieceCount(1), GetRemainingPieceCount(2));

        currentTurn = 2;
        SwitchTurn();

        retryButton.SetActive(true);
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

        // Multiplayer piece ownership is fixed by ActorNumber (Player.cs: actor 1 = Black, actor 2 =
        // White), so every client builds the identical board locally instead of spawning pieces over
        // the network.
        boardGenerator.GeneratePieces(PieceType.Black, PieceType.White);
        ServiceLocator.Get<GamePageManager>().GamePage.InitPiecesLeft(GetRemainingPieceCount(1), GetRemainingPieceCount(2));

        currentTurn = 1;
        if (PhotonNetwork.IsMasterClient)
        {
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn);
        }

        retryButton.SetActive(false);

        yield return new WaitForSeconds(1f);
    }

    private bool HasBothPlayerReady()
    {
        return players[0] != null && players[1] != null;
    }

    public void HandleTurnMissCount()
    {
        if(gameMode == GameModeType.Multiplayer && players[currentTurn - 1].PhotonView.IsMine)
        {
            players[currentTurn - 1].UpdateTurnMissCount();
            if (players[currentTurn - 1].TurnMissCount >= maxTurnMissCount)
            {
                int winner = currentTurn == 1 ? 2 : 1;
                gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner, "out of time");
            }
            else
            {
                SwitchTurn();
            }
        }
        else if(gameMode != GameModeType.Multiplayer)
        {
            players[currentTurn - 1].UpdateTurnMissCount();
            if (players[currentTurn - 1].TurnMissCount >= maxTurnMissCount)
            {
                int winner = currentTurn == 1 ? 2 : 1;
                GameOver(winner, "out of time");
            }
            else
            {
                SwitchTurn();
            }
        }
    }

    public void SwitchTurn()
    {
        if(gameMode == GameModeType.Multiplayer)
        {
            int nextTurn = currentTurn == 1 ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn);
        }
        else
        {
            players[currentTurn - 1].ResetPlayer();
            timer.ResetTimer();

            currentTurn = (currentTurn == 1) ? 2 : 1;
            pieceType = players[currentTurn - 1].PieceType;
            ServiceLocator.Get<GamePageManager>().GamePage.SetActiveTurn(currentTurn);

            if (!players[currentTurn - 1].CanPlay())
            {
                int winner = (currentTurn == 1) ? 2 : 1;
                GameOver(winner, "no legal moves left");
                return;
            }

            timer.StartTimer();
        }
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn)
    {
        players[currentTurn - 1].ResetPlayer();
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
        if( gameMode == GameModeType.Multiplayer && PhotonNetwork.IsMasterClient)
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
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.DestroyAll();
            }

            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.LeaveRoom();
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
