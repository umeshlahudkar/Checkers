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


    private readonly List<MovablePiece> selectedPieceToMove = new List<MovablePiece>();

    public void Play()
    {
        movablePieces.Clear();
        killerPieces.Clear();
        doubleKillerPieces.Clear();
        selectedPieceToMove.Clear();
        ResetHighlightBlock();

        if (GameplayController.Instance.CanMove(pieceType))
        {
            GameplayController.Instance.CheckMovablePieces(pieceType, movablePieces);

            if(movablePieces.Count > 0)
            {
                //HighlightMovablePieceBlock();
                SetMovablePosition();
                MovePiece();
            }
        }
        else
        {
            Debug.Log(".........Computer lose, can not move.........");
        }
    }

    private void MovePiece()
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
                Debug.Log("double killed by protecting own life");
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
                Debug.Log("double killed by risking own life");
                return;
            }
        }

        /*
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            if (piece.safeKillerBlockPositions.Count > 0)
            {
                BoardPosition position = piece.safeKillerBlockPositions[0];
                GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                MovePiece(piece, position);
                Debug.Log("killed by protecting own life");
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
                Debug.Log("killed by risking own life");
                return;
            }
        }
        */

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

    private void MovePiece(Piece piece, BoardPosition boardPosition)
    {
        GameplayController.Instance.selectedPiece = piece;
        Block targetMovableBlock = GameplayController.Instance.board[boardPosition.row_ID, boardPosition.col_ID];
        StartCoroutine(GameplayController.Instance.HandlePieceMovement(targetMovableBlock));
    }

    private void SetMovablePosition()
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            piece.ResetAllList();
            GameplayController.Instance.CheckWhitePieceSafePosition(piece);
        }
    }

    private async void ResetHighlightBlock()
    {
        await System.Threading.Tasks.Task.Delay(1000);
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

public class MovablePiece
{
    public Piece piece;
    public List<BoardPosition> positions;

    public MovablePiece(Piece piece, List<BoardPosition> positions)
    {
        this.piece = piece;
        this.positions = positions;
    }
}
