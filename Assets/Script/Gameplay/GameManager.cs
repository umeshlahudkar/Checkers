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

    [SerializeField] private int turnMissCount = 0;
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

    public int TurnMissCount { get { return turnMissCount; } }
    public int MaxTurnMissCount { get { return maxTurnMissCount; } }

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
            for (int i = 0; i < 2; i++)
            {
                players[i] = Instantiate(playerPrefab, transform.position, Quaternion.identity);
                PieceType pieceType = (i + 1) == 1 ? PieceType.Black : PieceType.White;
                players[i].SetPlayer(i + 1, pieceType, false);
            }

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                players[i] = Instantiate(playerPrefab, transform.position, Quaternion.identity);
                PieceType pieceType = (i + 1) == 1 ? PieceType.Black : PieceType.White;
                players[i].SetPlayer(i + 1, pieceType, ((i+1) == 2));
            }

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(players[0].PieceType, players[1].PieceType);

            currentTurn = 2;
            SwitchTurn();
        }

        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
    }

    private IEnumerator PrepareOnlineMode()
    {
        boardGenerator.GenerateBoard();
        PhotonNetwork.Instantiate(playerPrefab.name, transform.position, Quaternion.identity);

        PlayerInfo player1 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
        PlayerInfo player2 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

        GameplayUIController.Instance.ShowPlayerInfo(player1, player2);

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
    }

    private bool HasBothPlayerReady()
    {
        return players[0] != null && players[1] != null;
    }

    public void UpdateTurnMissCount()
    {
        if(currentTurn == actorNumber)
        {
            turnMissCount++;
            CheckForMissTurnExceed();
        }
    }

    private void CheckForMissTurnExceed()
    {
        if (turnMissCount >= maxTurnMissCount)
        {
            turnMissCount = 0;
            PieceType winner = pieceType == PieceType.White ? PieceType.Black : PieceType.White;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, (int)winner);
        }
        else
        {
            SwitchTurn();
        }
    }

    public void ResetTurnMissCount()
    {
        turnMissCount = 0;
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
                Debug.Log("...Player loose..." + currentTurn);
                return;
            }

            timer.StartTimer();
        }
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn)
    {
        currentTurn = nextTurn;

        //if (!GameplayController.Instance.CheckMoves((actorNumber == CurrentTurn)))
        if (players[currentTurn - 1].PhotonView.IsMine && !players[currentTurn - 1].CanPlay())
        {
            PieceType winner = pieceType == PieceType.White ? PieceType.Black : PieceType.White;
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All, (int)winner);
        }
    }

    [PunRPC]
    public void GameOver(int winner)
    {
        PieceType winnerPieceType = (PieceType)winner;

        if(winnerPieceType == pieceType)
        {
            GameplayUIController.Instance.ToggleGameWinScreen(true);
        }
        else
        {
            GameplayUIController.Instance.ToggleGameLoseScreen(true);
        }

        SetGameOver();
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
        turnMissCount = 0;
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
