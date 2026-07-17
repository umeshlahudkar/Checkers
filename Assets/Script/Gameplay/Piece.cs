using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Piece : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Button button;

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
        thisTransform.position = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.position;
        thisTransform.sizeDelta = ServiceLocator.Get<GameplayController>().board[rowID, columID].ThisTransform.sizeDelta;
        thisTransform.localScale = Vector3.one;

        ServiceLocator.Get<GameplayController>().board[rowID, columID].SetBlockPiece(true, this);

        bool isOwnPiece = ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer
            || ServiceLocator.Get<GameManager>().GetPlayer(playerID).PhotonView.IsMine;

        if(isOwnPiece)
        {
            button.interactable = true;
        }
    }

    public void SetCrownKing()
    {
        isCrownedKing = true;
        crownImage.gameObject.SetActive(true);
        ServiceLocator.Get<AudioManager>().PlayCrownKingSound();
    }

    public void Destroy()
    {
        ServiceLocator.Get<GameplayController>().board[rowID, columID].SetBlockPiece(false, null);
        blackPieceImage.gameObject.SetActive(false);
        whitePieceImage.gameObject.SetActive(false);
        crownImage.gameObject.SetActive(false);

        ServiceLocator.Get<AudioManager>().PlayPieceKillSound();

        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();

        if (playerID == 2)
        {
            gameplayController.whitePieces.Remove(this);
        }
        else
        {
            gameplayController.blackPieces.Remove(this);
        }

        ServiceLocator.Get<GamePageManager>().GamePage.UpdatePiecesLeft(gameplayController.blackPieces.Count, gameplayController.whitePieces.Count);

        Destroy(gameObject);
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

    public RectTransform ThisTransform { get { return thisTransform; } }

    public void OnClick()
    {
        if(playerID == ServiceLocator.Get<GameManager>().CurrentTurn)
        {
            ServiceLocator.Get<GameManager>().GetPlayer(playerID).OnHighlightedPieceClick(this);
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
