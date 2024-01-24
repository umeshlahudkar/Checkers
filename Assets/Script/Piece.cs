using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Piece : MonoBehaviour
{
    [SerializeField] private Image pieceImage;

    [SerializeField] private PieceType pieceType;

    [SerializeField] private int rowID;
    [SerializeField] private int columID;


    public void SetBlock(int row, int colum, PieceType pieceType, Sprite sprite)
    {
        columID = colum;
        rowID = row;
        this.pieceType = pieceType;
        pieceImage.sprite = sprite;
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

    public void OnClick()
    {
        if(GameManager.instance.pieceType == pieceType)
        {
            GameplayController.instance.CheckNextMove(this);
        }
    }

}

public enum PieceType
{
    None = 0,
    White,
    Black
}
