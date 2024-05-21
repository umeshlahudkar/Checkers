using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections.Generic;

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
    [SerializeField] private int playerID;


    [Header("Piece AI")]
    [HideInInspector] public List<BoardPosition> movableBlockPositions = new();
    [HideInInspector] public List<BoardPosition> safeMovableBlockPositions = new();

    [HideInInspector] public List<BoardPosition> killerBlockPositions = new();
    [HideInInspector] public List<BoardPosition> safeKillerBlockPositions = new();

    [HideInInspector] public List<BoardPosition> doubleKillerBlockPositions = new();
    [HideInInspector] public List<BoardPosition> safeDoubleKillerBlockPositions = new();


    [PunRPC]
    public void SetPiece(int _playerID, int _row, int _colum, int _pieceType)
    {
        playerID = _playerID;
        columID = _colum;
        rowID = _row;
        pieceType = (PieceType)_pieceType;
        isCrownedKing = false;

        if(pieceType == PieceType.White)
        {
            whitePieceImage.gameObject.SetActive(true);
        }
        else
        {
            blackPieceImage.gameObject.SetActive(true);
        }

        thisTransform.SetParent(GameObject.Find("Piece Holder").transform);
        thisTransform.position = GameplayController.Instance.board[rowID, columID].ThisTransform.position;
        thisTransform.sizeDelta = GameplayController.Instance.board[rowID, columID].ThisTransform.sizeDelta;
        thisTransform.localScale = Vector3.one;

        GameplayController.Instance.board[rowID, columID].SetBlockPiece(true, this);

        if((GameManager.Instance.GameMode == GameMode.Online && photonView.IsMine) || GameManager.Instance.GameMode != GameMode.Online)
        {
            button.interactable = true;
        }
    }

    [PunRPC]
    public void SetCrownKing()
    {
        isCrownedKing = true;
        crownImage.gameObject.SetActive(true);
        AudioManager.Instance.PlayCrownKingSound();
    }

    public void ResetCrownKing()
    {
        isCrownedKing = false;
        crownImage.gameObject.SetActive(false);
    }

    [PunRPC]
    public void Destroy()
    {
        GameplayController.Instance.board[rowID, columID].SetBlockPiece(false, null);
        blackPieceImage.gameObject.SetActive(false);
        whitePieceImage.gameObject.SetActive(false);
        crownImage.gameObject.SetActive(false);

        AudioManager.Instance.PlayPieceKillSound();

        if (playerID == 2)
        {
            GameplayController.Instance.whitePieces.Remove(this);
        }
        else
        {
            GameplayController.Instance.blackPieces.Remove(this);
        }

        if (GameManager.Instance.GameMode == GameMode.Online && photonView.IsMine)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
        else if(GameManager.Instance.GameMode != GameMode.Online)
        {
            Destroy(gameObject);
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

    public int Player_ID
    {
        get { return playerID; }
    }

    public PieceType PieceType { get { return pieceType; } }

    public PhotonView PhotonView { get { return photonView; } }

    public void ToggleInteractable(bool toggle)
    {
        button.interactable = toggle;
    }

    public void OnClick()
    {
        if(playerID == GameManager.Instance.CurrentTurn)
        {
            GameManager.Instance.GetPlayer(playerID).OnHighlightedPieceClick(this);
        }
    }

    public void ResetAllList()
    {
        movableBlockPositions.Clear();
        safeMovableBlockPositions.Clear();

        killerBlockPositions.Clear();
        safeKillerBlockPositions.Clear();

        doubleKillerBlockPositions.Clear();
        safeDoubleKillerBlockPositions.Clear();
    }
}

public enum PieceType
{
    None = 0,
    White,
    Black
}
