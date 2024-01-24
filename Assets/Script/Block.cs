using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Image blockImage;
    [SerializeField] private Image highlightImage;

    [SerializeField] private int columID;
    [SerializeField] private int rowID;

    [SerializeField] private bool isPiecePresent;
    private Piece piece;
    private bool isNextMoveHighlighted;
    private bool isNextToNextHighlighted;


    public void SetBlock(int row, int colum, Sprite sprite, bool isPiece, Piece piece = null)
    {
        columID = colum;
        rowID = row;
        blockImage.sprite = sprite;
        isPiecePresent = isPiece;
        this.piece = piece;
        isNextMoveHighlighted = false;
    }

    public void HighlightPieceBlock()
    {
        highlightImage.gameObject.SetActive(true);
    }

    public void HighlightNextMoveBlock(bool nextToNextHighlighted = false)
    {
        isNextMoveHighlighted = true;
        isNextToNextHighlighted = nextToNextHighlighted;
        highlightImage.gameObject.SetActive(true);
    }

    public void ResetBlock()
    {
        isNextMoveHighlighted = false;
        highlightImage.gameObject.SetActive(false);
    }

    public void OnClick()
    {
        if(isNextMoveHighlighted)
        {
            Debug.Log("block clicked");
            GameplayController.instance.MovePiece(this);
        }
    }

    public bool IsNextToNextHighlighted
    {
        get { return isNextToNextHighlighted; }
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
