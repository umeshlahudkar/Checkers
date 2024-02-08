using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckerAI : MonoBehaviour
{
    private readonly PieceType pieceType = PieceType.White;
    private readonly List<Piece> movablePieces = new();

    public bool CanPlay()
    {
        if (GameplayController.Instance.CanMove(pieceType))
        {
            Invoke(nameof(PlayTurn), 0.5f);
            return true;
        }
        return false;
    }

    private void PlayTurn()
    {
        movablePieces.Clear();
        GameplayController.Instance.CheckMovablePieces(pieceType, movablePieces);

        if (movablePieces.Count > 0)
        {
            SetMovablePosition();
            CheckPieceMove();
        }
    }

    private void SetMovablePosition()
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            piece.ResetAllList();
            GameplayController.Instance.SetPiecePosition(piece);
        }
    }

    private void CheckPieceMove()
    {
        movablePieces.Shuffle();

        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.safeDoubleKillerBlockPositions.Count > 0)
            {
                BoardPosition position = piece.safeDoubleKillerBlockPositions[0];
                GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                MovePiece(piece, position);
                return;
            }
        }

        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.doubleKillerBlockPositions.Count > 0)
            {
                BoardPosition position = piece.doubleKillerBlockPositions[0];
                GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                MovePiece(piece, position);
                return;
            }
        }

        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.safeKillerBlockPositions.Count > 0)
            {
                BoardPosition position = piece.safeKillerBlockPositions[0];
                GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                MovePiece(piece, position);
                return;
            }
        }

        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.killerBlockPositions.Count > 0)
            {
                BoardPosition position = piece.killerBlockPositions[0];
                GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                MovePiece(piece, position);
                return;
            }
        }


        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if(piece.safeMovableBlockPositions.Count > 0)
            {
                MovePiece(piece, piece.safeMovableBlockPositions[0]);
                return;
            }
        }


        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.movableBlockPositions.Count > 0)
            {
                MovePiece(piece, piece.movableBlockPositions[0]);
                return;
            }
        }
    }

    public void MovePiece(Piece piece, BoardPosition boardPosition)
    {
        GameplayController.Instance.selectedPiece = piece;
        Block targetMovableBlock = GameplayController.Instance.board[boardPosition.row_ID, boardPosition.col_ID];
        StartCoroutine(GameplayController.Instance.HandlePieceMovement(targetMovableBlock));
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

