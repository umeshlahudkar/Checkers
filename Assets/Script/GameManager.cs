using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        instance = this;
    }

    private PieceType pieceType;
    private int currentTurn;
    private int actorNumber;
    public BoardGenerator boardGenerator;

    private PhotonView gameManagerPhotonView;

    public int ActorNumber { get { return actorNumber; } }

    public int CurrentTurn { get { return currentTurn; } }

    public PieceType PieceType { get { return pieceType; } }

    private void Start()
    {
        gameManagerPhotonView = gameObject.GetComponent<PhotonView>();
        actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        if(PhotonNetwork.IsMasterClient)
        {
            currentTurn = actorNumber;
            pieceType = PieceType.Black;
        }
        else
        {
            pieceType = PieceType.White;
        }

        boardGenerator.GenerateBoard();
        boardGenerator.GeneratePieces(pieceType);

        if (PhotonNetwork.IsMasterClient)
        {
            GameplayController.instance.OnBoardReady();
        }


        Player[] players = PhotonNetwork.PlayerList;
        string player2_name = string.Empty;

        foreach(Player player in players)
        {
            if(player.ActorNumber != actorNumber)
            {
                player2_name = player.NickName;
            }
        }

        UIController.instance.ShowPlayerInfo(PhotonNetwork.NickName, player2_name);
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

        if(!GameplayController.instance.CheckMoves((actorNumber == CurrentTurn)))
        {
            gameManagerPhotonView.RPC(nameof(GameOver), RpcTarget.All);
        }
    }

    [PunRPC]
    public void GameOver()
    {
        
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
        GameplayController.instance.board[row, col].SetBlockPiece((viewId != -1), piece);
    }

}
