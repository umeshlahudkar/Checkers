using System.Collections;
using UnityEngine;
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

    private GameState gameState = GameState.Waiting;
    private GameModeType gameMode;
    private bool isReadyToLeaveGameplay = false;

    private readonly int maxTurnMissCount = 3;

    public GameState GameState 
    { 
        get { return gameState; }
    }

    public GameModeType GameMode
    {
        get { return gameMode; }
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
            PieceType player1_PieceType = (PieceType)Random.Range(1, 3);
            PieceType player2_PieceType = (player1_PieceType == PieceType.White) ? PieceType.Black : PieceType.White;

            for (int i = 0; i < 2; i++)
            {
                players[i] = Instantiate(humanPlayerPrefab, transform.position, Quaternion.identity);
                players[i].SetPlayer(i + 1, (i + 1 == 1) ? player1_PieceType:player2_PieceType);
            }

            ServiceLocator.Get<GameplayUIController>().ShowPlayerInfo(player1_PieceType.ToString(), ServiceLocator.Get<ProfileManager>().GetPieceAvtar(player1_PieceType),
                      player2_PieceType.ToString(), ServiceLocator.Get<ProfileManager>().GetPieceAvtar(player2_PieceType));

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();

            ServiceLocator.Get<GameplayUIController>().SetUpScreens();
            ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        }
        else
        {
            PieceType player1_PieceType = (PieceType)Random.Range(1, 3);
            PieceType player2_PieceType = (player1_PieceType == PieceType.White) ? PieceType.Black : PieceType.White;

            players[0] = Instantiate(humanPlayerPrefab, transform.position, Quaternion.identity);
            players[0].SetPlayer(1, player1_PieceType);

            players[1] = Instantiate(botPlayerPrefab, transform.position, Quaternion.identity);
            players[1].SetPlayer(2, player2_PieceType);

            ServiceLocator.Get<GameplayUIController>().ShowPlayerInfo(ServiceLocator.Get<ProfileManager>().GetUserName(), ServiceLocator.Get<ProfileManager>().GetProfileAvtar(),
                      "Computer", ServiceLocator.Get<ProfileManager>().GetComputerAvtar());

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();

            ServiceLocator.Get<GameplayUIController>().SetUpScreens();
            ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        }
    }

    private IEnumerator PrepareOnlineMode()
    {
        boardGenerator.GenerateBoard();
        PhotonNetwork.Instantiate("Prefab/" + humanPlayerPrefab.name, transform.position, Quaternion.identity);

        PlayerInfo player1 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
        PlayerInfo player2 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

        ServiceLocator.Get<GameplayUIController>().ShowPlayerInfo(player1.userName, ServiceLocator.Get<ProfileManager>().GetAvtar(player1.avtarIndex),
            player2.userName, ServiceLocator.Get<ProfileManager>().GetAvtar(player2.avtarIndex));

        while(!HasBothPlayerReady())
        {
            yield return null;
        }

        boardGenerator.GeneratePieces();

        currentTurn = 1;
        if (PhotonNetwork.IsMasterClient)
        {
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn);
        }

        ServiceLocator.Get<GameplayUIController>().SetUpScreens();

        yield return new WaitForSeconds(1f);

        ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
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
                gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner);
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
                GameOver(winner);
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
            ServiceLocator.Get<GameplayUIController>().SetActiveTurn(currentTurn);

            if (!players[currentTurn - 1].CanPlay())
            {
                int winner = (currentTurn == 1) ? 2 : 1;
                GameOver(winner);
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
        ServiceLocator.Get<GameplayUIController>().SetActiveTurn(currentTurn);

        if (players[currentTurn - 1].PhotonView.IsMine && !players[currentTurn - 1].CanPlay())
        {
            int winner = (currentTurn == 1) ? 2 : 1;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, winner);
            return;
        }

        timer.StartTimer();
    }

    [PunRPC]
    public void GameOver(int winnerPlayerNumber)
    {
        SetGameOver();

        if (gameMode == GameModeType.Multiplayer)
        {
            if(players[winnerPlayerNumber-1].PhotonView.IsMine)
            {
                ServiceLocator.Get<GameplayUIController>().ToggleGameWinScreen(true);
            }
            else
            {
                ServiceLocator.Get<GameplayUIController>().ToggleGameLoseScreen(true);
            }
        }
        else if (gameMode == GameModeType.VsPlayer)
        {
            string winnerName = players[winnerPlayerNumber - 1].PieceType.ToString();
            string loserName = players[(winnerPlayerNumber == 1 ? 2 : 1) - 1].PieceType.ToString();

            ServiceLocator.Get<GameplayUIController>().ToggleGameOverScreen(true, winnerName, loserName);
        }
        else if (gameMode == GameModeType.VsBot)
        {
            if (winnerPlayerNumber == 1)
            {
                ServiceLocator.Get<GameplayUIController>().ToggleGameWinScreen(true);
            }
            else
            {
                ServiceLocator.Get<GameplayUIController>().ToggleGameLoseScreen(true);
            }
        }
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
        ServiceLocator.Get<GameplayUIController>().DisableAllScreen();
    }

    public IEnumerator Rematch()
    {
        ServiceLocator.Get<PersistentUI>().loadingScreen.ActivateLoadingScreen("Starting match");
        ResetGameplay();
        yield return new WaitForSeconds(2f);
        InitializeGame();
    }
}

public enum GameState
{
    Waiting,
    Playing,
    Ending
}
