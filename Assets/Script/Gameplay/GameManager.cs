using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

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
        StartCoroutine(InitializeGame());
    }


    private IEnumerator InitializeGame()
    {
        yield return new WaitForSeconds(3f);
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

        if(!GameplayController.Instance.CheckMoves((actorNumber == CurrentTurn)))
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
    }


    public void UpdateGrid(int row, int col, Piece piece)
    {
        int viewId = -1;
        if(piece != null)
        {
            viewId = piece.GetComponent<PhotonView>().ViewID;
        }
        gameManagerPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, row, col, viewId);
    }

    [PunRPC]
    public void UpdateGrid(int row, int col, int viewId)
    {
        Piece piece = null;
        if(viewId != -1)
        {
            piece = PhotonView.Find(viewId).GetComponent<Piece>();
        }
        GameplayController.Instance.board[row, col].SetBlockPiece((viewId != -1), piece);
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

    public void Rematch()
    {
        ResetGameplay();
        StartCoroutine(InitializeGame());
    }

}
