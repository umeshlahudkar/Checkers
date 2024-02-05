using System.Collections;
using UnityEngine;
using Photon.Pun;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameDataSO gameDataSO;
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PhotonView gameManagerPhotonView;
    [SerializeField] private GameplayTimer timer;

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
            actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            if (PhotonNetwork.IsMasterClient)
            {
                currentTurn = actorNumber;
                pieceType = PieceType.Black;
            }
            else
            {
                pieceType = PieceType.White;
            }

            PlayerInfo player1 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.ownPlayer : gameDataSO.opponentPlayer;
            PlayerInfo player2 = gameDataSO.ownPlayer.isMasterClient ? gameDataSO.opponentPlayer : gameDataSO.ownPlayer;

            GameplayUIController.Instance.ShowPlayerInfo(player1, player2);

            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces(pieceType);

            if (PhotonNetwork.IsMasterClient)
            {
                gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn);
            }
        }
        else if(gameMode == GameMode.PVP)
        {
            GameplayUIController.Instance.DisablePlayerInfo();
            boardGenerator.GenerateBoard();
            boardGenerator.GeneratePieces();

            currentTurn = 1;
            pieceType = PieceType.Black;
            GameplayController.Instance.CheckMoves();
        }

        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
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
            currentTurn = (currentTurn == 1) ? 2 : 1;
            pieceType = (pieceType == PieceType.White) ? PieceType.Black : PieceType.White;

            if (!GameplayController.Instance.CheckMoves())
            {
                if(pieceType == PieceType.White)
                {
                    GameplayUIController.Instance.ToggleGameWinScreen(true);
                }
                else
                {
                    GameplayUIController.Instance.ToggleGameLoseScreen(true);
                }
            }
        }
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn)
    {
        currentTurn = nextTurn;
        timer.ResetTimer();
        GameplayUIController.Instance.PlayHighlightAnimation(currentTurn);

        if (!GameplayController.Instance.CheckMoves((actorNumber == CurrentTurn)))
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
        GameplayUIController.Instance.StopPlayerHighlightAnim();
    }

    private void ResetGameManager()
    {
        pieceType = PieceType.None;
        currentTurn = -1;
        actorNumber = -1;
        turnMissCount = 0;
        gameState = GameState.Waiting;
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
