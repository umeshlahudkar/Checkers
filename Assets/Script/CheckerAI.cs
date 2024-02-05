using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerAI : MonoBehaviour
{
    private readonly PieceType pieceType = PieceType.White;
    private readonly List<Piece> movablePieces = new List<Piece>();
    private readonly List<Block> highlightBlocks = new List<Block>();
    private readonly List<Piece> killerPieces = new List<Piece>();
    private readonly List<Piece> doubleKillerPieces = new List<Piece>();

    public void Play()
    {
        movablePieces.Clear();
        killerPieces.Clear();
        doubleKillerPieces.Clear();
        ResetHighlightBlock();

        if (GameplayController.Instance.CanMove(pieceType))
        {
            GameplayController.Instance.CheckMovablePieces(pieceType, movablePieces);

            if(movablePieces.Count > 0)
            {
                Debug.Log("Movable piece Count : " + movablePieces.Count);
                HighlightMovablePieceBlock();
                SetMovablePosition();
                CheckForKillerPieces();

                if(killerPieces.Count > 0)
                {
                    Debug.Log("Killer piece Count : " + killerPieces.Count);
                    //PrintKillerPieces();
                    CheckForDoubleKillerPieces();
                    if(doubleKillerPieces.Count > 0)
                    {
                        Debug.Log("Double_Killer piece Count : " + doubleKillerPieces.Count);
                    }
                }
            }
        }

        GameManager.Instance.SwitchTurn();
    }

    private void CheckForDoubleKillerPieces()
    {
        for (int i = 0; i < killerPieces.Count; i++)
        {
            if(GameplayController.Instance.CanDoubleKill(killerPieces[i]))
            {
                doubleKillerPieces.Add(killerPieces[i]);
            }
        }
    }

    private void PrintKillerPieces()
    {
        for (int i = 0; i < killerPieces.Count; i++)
        {
            Debug.Log("Killer Piece : " + GameplayController.Instance.board[killerPieces[i].Row_ID, killerPieces[i].Coloum_ID].gameObject.name);
                
        }
    }

    private void CheckForKillerPieces()
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            if(GameplayController.Instance.CanKillOpponentPiece(movablePieces[i]))
            {
                killerPieces.Add(movablePieces[i]);
            }
        }
    }

    private void SetMovablePosition()
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            movablePieces[i].movableBlockPositions.Clear();
            GameplayController.Instance.SetMovablePosition(movablePieces[i]);
            //Debug.Log(GameplayController.Instance.board[movablePieces[i].Row_ID, movablePieces[i].Coloum_ID].gameObject.name + "  " +
            //     movablePieces[i].movableBlockPositions.Count);
        }
    }

    private void ResetHighlightBlock()
    {
        for (int i = 0; i < highlightBlocks.Count; i++)
        {
            highlightBlocks[i].ResetBlock();
        }
        highlightBlocks.Clear();
    }

    private void HighlightMovablePieceBlock()
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID].HighlightPieceBlock();
            highlightBlocks.Add(GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID]);
        }
    }
}

public struct BoardPosition
{
    public int row_ID;
    public int col_ID;

    public BoardPosition(int row, int col)
    {
        row_ID = row;
        col_ID = col;
    }
}
