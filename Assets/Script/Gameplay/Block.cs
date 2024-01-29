using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;

    [SerializeField] private Button button;

    [SerializeField] private Image blockImage;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Image targetImage;

    [SerializeField] private int columID;
    [SerializeField] private int rowID;

    [SerializeField] private bool isPiecePresent;
    private Piece piece;

    private bool isTargetBlockHighlighted;
    private bool isNextTargetBlockHighlighted;

    private float startTime = 0f;
    private float animationDuration = 1f;
    private bool animFlag;

    [Header("Highlight Anim Data")]
    private float maxAlpha = 1f;
    private float minAlpha = 0.5f;

    [Header("Target Image Anim Data")]
    private float maxScale = 1f;
    private float minScale = 0.5f;

    public void SetBlock(int row, int colum, Sprite sprite)
    {
        columID = colum;
        rowID = row;
        blockImage.sprite = sprite;
        //isPiecePresent = isPiece;
        //this.piece = piece;
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

    private void Update()
    {
        if(highlightImage.enabled)
        {
            PlayHighlightImageAnim();
        }

        if (targetImage.enabled)
        {
            PlayTargetImageAnim();
        }
    }

    private void PlayHighlightImageAnim()
    {
        float t = (Time.time - startTime) / animationDuration;
        float newAlpha;

        if (animFlag)
        {
            newAlpha = Mathf.Lerp(maxAlpha, minAlpha, t);
        }
        else
        {
            newAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        SetHighlightImageAlpha(newAlpha);

        if (t >= animationDuration)
        {
            animFlag = !animFlag;
            startTime = Time.time;
        }
    }

    private void PlayTargetImageAnim()
    {
        float t = (Time.time - startTime) / animationDuration;
        float newScale;

        if (animFlag)
        {
            newScale = Mathf.Lerp(maxScale, minScale, t);
        }
        else
        {
            newScale = Mathf.Lerp(minScale, maxScale, t);
        }

        targetImage.rectTransform.localScale = new Vector3(newScale, newScale, newScale);

        if (t >= animationDuration)
        {
            animFlag = !animFlag;
            startTime = Time.time;
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
        startTime = Time.time;
        animFlag = false;

        highlightImage.gameObject.SetActive(true);
        button.interactable = true;
    }

    public void HighlightNextMoveBlock(bool nextToNextHighlighted = false)
    {
        startTime = Time.time;
        animFlag = false;

        isTargetBlockHighlighted = true;
        isNextTargetBlockHighlighted = nextToNextHighlighted;
        targetImage.gameObject.SetActive(true);
        button.interactable = true;
    }

    public void ResetBlock()
    {
        button.interactable = false;
        isTargetBlockHighlighted = false;
        highlightImage.gameObject.SetActive(false);
        targetImage.gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if(isTargetBlockHighlighted)
        {
            GameplayController.Instance.MovePiece(this);
        }
    }

    public bool IsNextToNextHighlighted
    {
        get { return isNextTargetBlockHighlighted; }
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
