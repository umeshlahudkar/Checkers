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
    public void SetPiece(int row, int colum, int pieceType, float blockSize)
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
        thisTransform.position = GameplayController.Instance.board[row, colum].ThisTransform.position;
        thisTransform.sizeDelta = GameplayController.Instance.board[row, colum].ThisTransform.sizeDelta;
        thisTransform.localScale = Vector3.one;

        GameplayController.Instance.board[row, colum].SetBlockPiece(true, this);

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

    [PunRPC]
    public void Destroy()
    {
        GameplayController.Instance.board[rowID, columID].SetBlockPiece(false, null);
        blackPieceImage.gameObject.SetActive(false);
        whitePieceImage.gameObject.SetActive(false);
        crownImage.gameObject.SetActive(false);

        AudioManager.Instance.PlayPieceKillSound();

        if (pieceType == PieceType.White)
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
        else
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

    public PieceType PieceType { get { return pieceType; } }

    public PhotonView PhotonView { get { return photonView; } }

    public void OnClick()
    {
        if(( GameManager.Instance.GameMode == GameMode.Online && GameManager.Instance.ActorNumber == GameManager.Instance.CurrentTurn && pieceType == GameManager.Instance.PieceType) ||
            (GameManager.Instance.GameMode != GameMode.Online && pieceType == GameManager.Instance.PieceType))
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
