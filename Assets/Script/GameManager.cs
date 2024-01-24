using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private void Awake()
    {
        instance = this;

        turn = 1;
        pieceType = (PieceType)turn;
    }

    public PieceType pieceType;
    public int turn;

    public void ChangeTurn()
    {
        turn = turn == 1 ? 2 : 1;
        pieceType = (PieceType)turn;

        GameplayController.instance.CheckMoves();
    }

}
