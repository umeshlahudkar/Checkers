using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

            //boardGenerator.GenerateBoard();
            //GameplayController.instance.OnBoardReady();
        }
        else
        {
            pieceType = PieceType.White;
        }

        boardGenerator.GenerateBoard();
        boardGenerator.GeneratePieces(pieceType);
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

        if(currentTurn == actorNumber && !GameplayController.instance.CheckMoves())
        {
            Debug.Log("Game Over : looser : " + pieceType);
        }
    }

}
