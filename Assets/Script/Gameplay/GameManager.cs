using System.Collections;
using UnityEngine;
using Photon.Pun;


public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameDataSO gameDataSO;
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private PhotonView gameManagerPhotonView;

    private PieceType pieceType;
    private int currentTurn;
    private int actorNumber;

    public int ActorNumber { get { return actorNumber; } }

    public int CurrentTurn { get { return currentTurn; } }

    public PieceType PieceType { get { return pieceType; } }

    private void Start()
    {
        InitializeGame();
    }


    private void InitializeGame()
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

        UIController.Instance.ShowPlayerInfo(player1, player2);

        boardGenerator.GenerateBoard();
        boardGenerator.GeneratePieces(pieceType);

        if (PhotonNetwork.IsMasterClient)
        {
            //GameplayController.Instance.OnBoardReady();
            gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, currentTurn);
        }

        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
    }

    public void SwitchTurn()
    {
        int nextTurn = currentTurn == 1 ? 2 : 1;
        gameManagerPhotonView.RPC(nameof(ChangeTurn), RpcTarget.All, nextTurn);
    }

    [PunRPC]
    public void ChangeTurn(int nextTurn)
    {
        currentTurn = nextTurn;
        UIController.Instance.PlayHighlightAnimation(currentTurn);

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
            UIController.Instance.ToggleGameWinScreen(true);
        }
        else
        {
            UIController.Instance.ToggleGameLoseScreen(true);
        }

        UIController.Instance.StopPlayerHighlightAnim();
    }

    private void ResetGameManager()
    {
        pieceType = PieceType.None;
        currentTurn = -1;
        actorNumber = -1;
    }

    private void ResetGameplay()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
        }
        ResetGameManager();
        GameplayController.Instance.ResetGameplay();
        UIController.Instance.DisableAllScreen();
    }

    public IEnumerator Rematch()
    {
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Starting match");
        ResetGameplay();
        yield return new WaitForSeconds(2f);
        InitializeGame();
    }

}
