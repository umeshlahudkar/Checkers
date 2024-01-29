using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Piece : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Button button;
    [SerializeField] private PhotonView photonView;

    [SerializeField] private Image whitePieceImage;
    [SerializeField] private Image blackPieceImage;
    [SerializeField] private Image crownImage;
    [SerializeField] private PieceType pieceType;

    [SerializeField] private int rowID;
    [SerializeField] private int columID;

    [SerializeField] private bool isCrownedKing;
    

    [PunRPC]
    public void SetBlock(int row, int colum, int pieceType, float blockSize)
    {
        columID = colum;
        rowID = row;
        this.pieceType = (PieceType)pieceType;
        isCrownedKing = false;

        if(this.pieceType == PieceType.White)
        {
            whitePieceImage.gameObject.SetActive(true);
        }
        else
        {
            blackPieceImage.gameObject.SetActive(true);
        }

        thisTransform.SetParent(GameObject.Find("Piece Holder").transform);
        thisTransform.sizeDelta = new Vector2(blockSize, blockSize);
        thisTransform.localScale = Vector3.one;

        GameplayController.Instance.board[row, colum].SetBlockPiece(true, this);

        if(photonView.IsMine)
        {
            button.interactable = true;
        }
    }

    [PunRPC]
    public void SetCrownKing()
    {
        isCrownedKing = true;
        crownImage.gameObject.SetActive(true);
    }

    [PunRPC]
    public void Destroy()
    {
        GameplayController.Instance.board[rowID, columID].SetBlockPiece(false, null);

        if (pieceType == PieceType.White)
        {
            GameplayController.Instance.whitePieces.Remove(this);
        }
        else
        {
            GameplayController.Instance.blackPieces.Remove(this);
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

    public bool IsCrownedKing
    {
        get { return isCrownedKing; }
    }

    public int Row_ID 
    { 
        get { return rowID; }
        set { rowID = value; }
    }

    public int Coloum_ID 
    { 
        get { return columID; }
        set { columID = value; }
    }

    public PieceType PieceType { get { return pieceType; } }

    public PhotonView PhotonView { get { return photonView; } }

    public void OnClick()
    {
        if(GameManager.Instance.ActorNumber == GameManager.Instance.CurrentTurn && pieceType == GameManager.Instance.PieceType)
        {
            GameplayController.Instance.CheckNextMove(this);
        }
    }

}

public enum PieceType
{
    None = 0,
    White,
    Black
}