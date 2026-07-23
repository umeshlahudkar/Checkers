using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;

    [SerializeField] private Button button;

    [SerializeField] private Image blockImage;
    [SerializeField] private Image highlightImage;
    //[SerializeField] private Image highlightHoleImage;
    [SerializeField] private Image targetImage;
    //[SerializeField] private Image targetHoleImage;
    [SerializeField] private Image lastMoveImage;
    [SerializeField] private Image hintImage;

    [SerializeField] private int columID;
    [SerializeField] private int rowID;

    [SerializeField] private bool isPiecePresent;
    private Piece piece;

    private bool isTargetBlockHighlighted;
    private bool isNextTargetBlockHighlighted;

    // The position of the piece a click on this (highlighted-as-a-capture) block would capture.
    // Can't be derived geometrically from this block's own position for a flying king, which may
    // capture from any distance along the diagonal, so it's set explicitly at highlight time.
    private BoardPosition capturedPosition;

    private readonly float pulseDuration = 1f;

    private readonly float maxAlpha = 1f;
    private readonly float minAlpha = 0.5f;

    private readonly float maxScale = 1f;
    private readonly float minScale = 0.6f;

    public void SetBlock(int row, int colum, Sprite sprite)
    {
        columID = colum;
        rowID = row;
        blockImage.sprite = sprite;
        isTargetBlockHighlighted = false;
        button.interactable = false;
    }

    public void SetBlockPiece(bool piecePresent, Piece piece)
    {
        isPiecePresent = piecePresent;
        this.piece = piece;

        if(this.piece != null)
        {
            this.piece.Row_ID = rowID;
            this.piece.Coloum_ID = columID;
        }
    }

    private void SetHighlightImageAlpha(float alpha)
    {
        Color currentColor = highlightImage.color;
        Color newColor = new(currentColor.r, currentColor.g, currentColor.b, alpha);
        highlightImage.color = newColor;
    }

    public void HighlightPieceBlock()
    {
        highlightImage.DOKill();
        SetHighlightImageAlpha(minAlpha);
        highlightImage.gameObject.SetActive(true);

        highlightImage.DOFade(maxAlpha, pulseDuration).SetLoops(-1, LoopType.Yoyo);
    }

    public void HighlightNextMoveBlock(bool nextToNextHighlighted = false)
    {
        isTargetBlockHighlighted = true;
        isNextTargetBlockHighlighted = nextToNextHighlighted;
        targetImage.gameObject.SetActive(true);
        button.interactable = true;

        targetImage.rectTransform.DOKill();
        targetImage.rectTransform.localScale = Vector3.one * minScale;
        targetImage.rectTransform.DOScale(maxScale, pulseDuration).SetLoops(-1, LoopType.Yoyo);
    }

    public void HighlightAsLastMove()
    {
        lastMoveImage.gameObject.SetActive(true);
    }

    public void ResetLastMoveHighlight()
    {
        lastMoveImage.gameObject.SetActive(false);
    }

    public void ShowHint()
    {
        hintImage.gameObject.SetActive(true);
    }

    public void ClearHint()
    {
        hintImage.gameObject.SetActive(false);
    }

    public void ResetBlock()
    {
        button.interactable = false;
        isTargetBlockHighlighted = false;

        highlightImage.DOKill();
        targetImage.rectTransform.DOKill();

        highlightImage.gameObject.SetActive(false);
        targetImage.gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if(isTargetBlockHighlighted)
        {
            ServiceLocator.Get<GameManager>().GetPlayer(ServiceLocator.Get<GameManager>().CurrentTurn).OnHighlightedTargetBlockClick(this);
        }
    }

    public bool IsNextToNextHighlighted
    {
        get { return isNextTargetBlockHighlighted; }
        set { isNextTargetBlockHighlighted = value; }
    }

    public BoardPosition CapturedPosition
    {
        get { return capturedPosition; }
        set { capturedPosition = value; }
    }

    public int Row_ID { get { return rowID; } }
    public int Coloum_ID { get { return columID; } }

    public bool IsPiecePresent
    { 
        get { return isPiecePresent; }
        set { isPiecePresent = value; }
    }

    public Piece Piece 
    { 
        get { return piece; }
        set { piece = value; }
    }

    public RectTransform ThisTransform { get { return thisTransform; } }
}
