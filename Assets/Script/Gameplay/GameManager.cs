using System.Collections;
using UnityEngine;
using Photon.Pun;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameDataSO gameDataSO;
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PhotonView gameManagerPhotonView;
    [SerializeField] private TimerController timer;

    [SerializeField] private Gameplay.Player playerPrefab;

    [SerializeField] private Gameplay.Player[] players = new Gameplay.Player[2];

    private PieceType pieceType;
    private int currentTurn;
    private int actorNumber;

    private GameState gameState = GameState.Waiting;
    private GameMode gameMode;

    private readonly int maxTurnMissCount = 3;

    public GameState GameState 
    { 
        get { return gameState; }
    }

    public GameMode GameMode
    {
        get { return gameMode; }
    }

    public int ActorNumber { get { return actorNumber; } }

    public int CurrentTurn { get { return currentTurn; } }

    public PieceType PieceType { get { return pieceType; } }


    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        gameMode = gameDataSO.gameMode;
        gameState = GameState.Playing;

        if (gameMode == GameMode.Online)
        {
            StartCoroutine(PrepareOnlineMode());
        }
        else if (gameMode == GameMode.PVP)
        {
            PieceType player1_PieceType = (PieceType)Random.Range(1, 3);
            PieceType player2_PieceType = (player1_PieceType == PieceType.White) ? PieceType.Black : PieceType.White;

            for (int i = 0; i < 2; i++)
            {
                players[i] = Instantiate(playerPrefab, transform.position, Quaternion.identity);
                players[i].SetPlayer(i + 1, (i + 1 == 1) ? player1_PieceType:player2_PieceType, false);
            }

            GameplayUIController.Instance.ShowPlayerInfo(player1_PieceType.ToString(), ProfileManager.Instance.GetPieceAvtar(player1_PieceType),
                      player2_PieceType.ToString(), ProfileManager.Instance.GetPieceAvtar(player2_PieceType));

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();

            PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        }
        else
        {
            PieceType player1_PieceType = (PieceType)Random.Range(1, 3);
            PieceType player2_PieceType = (player1_PieceType == PieceType.White) ? PieceType.Black : PieceType.White;

            for (int i = 0; i < 2; i++)
            {
                players[i] = Instantiate(playerPrefab, transform.position, Quaternion.identity);
                players[i].SetPlayer(i + 1, (i + 1 == 1) ? player1_PieceType : player2_PieceType, ((i+1) == 2));
            }

            GameplayUIController.Instance.ShowPlayerInfo(ProfileManager.Instance.GetUserName(), ProfileManager.Instance.GetProfileAvtar(),
                      "Computer", ProfileManager.Instance.GetComputerAvtar());

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();

            PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        }
    }

    private IEnumerator PrepareOnlineMode()
    {
        boardGenerator.GenerateBoard();
        PhotonNetwork.Instantiate(playerPrefab.name, transform.position, Quaternion.identity);

        PlayerInfo player1 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
        PlayerInfo player2 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

        GameplayUIController.Instance.ShowPlayerInfo(player1.userName, ProfileManager.Instance.GetAvtar(player1.avtarIndex),
            player2.userName, ProfileManager.Instance.GetAvtar(player2.avtarIndex));

        while(!HasBothPlayerReady())
        {
            yield return null;
        }

        boardGenerator.GeneratePieces();

        if (PhotonNetwork.IsMasterClient)
        {
            currentTurn = 1;
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn);
        }

        yield return new WaitForSeconds(1f);
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
    }

    private bool HasBothPlayerReady()
    {
        return players[0] != null && players[1] != null;
    }

    public void HandleTurnMissCount()
    {
        if(gameMode == GameMode.Online && players[currentTurn - 1].PhotonView.IsMine)
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
        else if(gameMode != GameMode.Online)
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
        if(gameMode == GameMode.Online)
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
        currentTurn = nextTurn;
        timer.ResetTimer();

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

        if (gameMode == GameMode.Online)
        {
            if(players[winnerPlayerNumber-1].PhotonView.IsMine)
            {
                GameplayUIController.Instance.ToggleGameWinScreen(true);
            }
            else
            {
                GameplayUIController.Instance.ToggleGameLoseScreen(true);
            }
        }
        else if (gameMode == GameMode.PVP)
        {
            string winnerName = players[winnerPlayerNumber - 1].PieceType.ToString();
            string loserName = players[(winnerPlayerNumber == 1 ? 2 : 1) - 1].PieceType.ToString();

            GameplayUIController.Instance.ToggleGameOverScreen(true, winnerName, loserName);
        }
        else if (gameMode == GameMode.PVC)
        {
            if (winnerPlayerNumber == 1)
            {
                GameplayUIController.Instance.ToggleGameWinScreen(true);
            }
            else
            {
                GameplayUIController.Instance.ToggleGameLoseScreen(true);
            }
        }
    }

    public void SetGameOver()
    {
        gameState = GameState.Ending;
        AudioManager.Instance.StopTimeTickingSound();
    }

    private void ResetGameManager()
    {
        pieceType = PieceType.None;
        currentTurn = -1;
        actorNumber = -1;
        gameState = GameState.Waiting;
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
        if( gameMode == GameMode.Online && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }
        ResetGameManager();
        GameplayController.Instance.ResetGameplay();
        GameplayUIController.Instance.DisableAllScreen();
    }

    public IEnumerator Rematch()
    {
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Starting match");
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
