using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayController : MonoBehaviour
{
    public static GameplayController instance;

    private void Awake()
    {
        instance = this;
    }

    public Block[,] board = new Block[8, 8];
    public List<Piece> whitePieces = new List<Piece>();
    public List<Piece> blackPieces = new List<Piece>();

    [SerializeField] private List<Block> highlightedBlocks;

    public Piece selectedPiece;


    public void OnBoardReady()
    {
        highlightedBlocks = new List<Block>();

        CheckMoves();
    }

    public void CheckMoves()
    {
        if (GameManager.instance.pieceType == PieceType.Black)
        {
            CheckBlackPieceMove();
        }
        else if (GameManager.instance.pieceType == PieceType.White)
        {
            CheckWhitePieceMove();
        }
    }

    public bool CheckWhitePieceMove()
    {
        bool blockFound = false;
        foreach (Piece piece in whitePieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            Block pieceBlock = board[row, coloum];

            if (row + 1 < 8 && coloum - 1 >= 0)
            {
                if (!board[row + 1, coloum - 1].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
                else if (board[row + 1, coloum - 1].IsPiecePresent && piece.PieceType != board[row + 1, coloum - 1].Piece.PieceType
                        && row + 2 < 8 && coloum - 2 >= 0 && !board[row + 2, coloum - 2].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
            }

            if (row + 1 < 8 && coloum + 1 < 8)
            {
                if (!board[row + 1, coloum + 1].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
                else if (board[row + 1, coloum + 1].IsPiecePresent && piece.PieceType != board[row + 1, coloum + 1].Piece.PieceType
                        && row + 2 < 8 && coloum + 2 >= 0 && !board[row + 2, coloum + 2].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
            }
        }
        return blockFound;
    }

    public bool CheckBlackPieceMove()
    {
        bool blockFound = false;
        foreach (Piece piece in blackPieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            Block pieceBlock = board[row, coloum];

            if (row - 1 >= 0 && coloum - 1 >= 0)
            {
                if (!board[row - 1, coloum - 1].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
                else if (board[row - 1, coloum - 1].IsPiecePresent && piece.PieceType != board[row - 1, coloum - 1].Piece.PieceType
                         && row - 2 < 8 && coloum - 2 >= 0 && !board[row - 2, coloum - 2].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
            }

            if (row - 1 >= 0 && coloum + 1 < 8)
            {
                if (!board[row - 1, coloum + 1].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
                else if (board[row - 1, coloum + 1].IsPiecePresent && piece.PieceType != board[row - 1, coloum + 1].Piece.PieceType
                         && row - 2 < 8 && coloum + 2 >= 0 && !board[row - 2, coloum + 2].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
            }
        }
        return blockFound;
    }

    private void ResetHighlightedBlocks(PieceType pieceType)
    {
        foreach (Block b in highlightedBlocks)
        {
            b.ResetBlock();
        }
        highlightedBlocks.Clear();
    }

    public void CheckNextMove(Piece piece)
    {
        ResetHighlightedBlocks(piece.PieceType);

        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;

        bool moveFound = false;

        if (piece.PieceType == PieceType.White)
        {
            if (row + 1 < 8 && coloum - 1 >= 0)
            {
                if (!board[row + 1, coloum - 1].IsPiecePresent)
                {
                    board[row + 1, coloum - 1].HighlightNextMoveBlock();
                    highlightedBlocks.Add(board[row + 1, coloum - 1]);
                    moveFound = true;
                }
                else if (board[row + 1, coloum - 1].IsPiecePresent && piece.PieceType != board[row + 1, coloum - 1].Piece.PieceType
                         && row + 2 < 8 && coloum - 2 >= 0 && !board[row + 2, coloum - 2].IsPiecePresent) 
                {
                    board[row + 2, coloum - 2].HighlightNextMoveBlock(true);
                    highlightedBlocks.Add(board[row + 2, coloum - 2]);
                    moveFound = true;
                }
            }

            if (row + 1 < 8 && coloum + 1 < 8)
            {
                if (!board[row + 1, coloum + 1].IsPiecePresent)
                {
                    board[row + 1, coloum + 1].HighlightNextMoveBlock();
                    highlightedBlocks.Add(board[row + 1, coloum + 1]);
                    moveFound = true;
                }
                else if (board[row + 1, coloum + 1].IsPiecePresent && piece.PieceType != board[row + 1, coloum + 1].Piece.PieceType
                         && row + 2 < 8 && coloum + 2 >= 0 && !board[row + 2, coloum + 2].IsPiecePresent)
                {
                    board[row + 2, coloum + 2].HighlightNextMoveBlock(true);
                    highlightedBlocks.Add(board[row + 2, coloum + 2]);
                    moveFound = true;
                }
            }
        }
        else if (piece.PieceType == PieceType.Black)
        {
            if (row - 1 >= 0 && coloum - 1 >= 0)
            {
                if (!board[row - 1, coloum - 1].IsPiecePresent)
                {
                    board[row - 1, coloum - 1].HighlightNextMoveBlock();
                    highlightedBlocks.Add(board[row - 1, coloum - 1]);
                    moveFound = true;
                }
                else if (board[row - 1, coloum - 1].IsPiecePresent && piece.PieceType != board[row - 1, coloum - 1].Piece.PieceType
                        && row - 2 < 8 && coloum - 2 >= 0 && !board[row - 2, coloum - 2].IsPiecePresent)
                {
                    board[row - 2, coloum - 2].HighlightNextMoveBlock(true);
                    highlightedBlocks.Add(board[row - 2, coloum - 2]);
                    moveFound = true;
                }
            }

            if (row - 1 >= 0 && coloum + 1 < 8)
            {
                if (!board[row - 1, coloum + 1].IsPiecePresent)
                {
                    board[row - 1, coloum + 1].HighlightNextMoveBlock();
                    highlightedBlocks.Add(board[row - 1, coloum + 1]);
                    moveFound = true;
                }
                else if (board[row - 1, coloum + 1].IsPiecePresent && piece.PieceType != board[row - 1, coloum + 1].Piece.PieceType
                       && row - 2 < 8 && coloum + 2 >= 0 && !board[row - 2, coloum + 2].IsPiecePresent)
                {
                    board[row - 2, coloum + 2].HighlightNextMoveBlock(true);
                    highlightedBlocks.Add(board[row - 2, coloum + 2]);
                    moveFound = true;
                }
            }
        }

        if (moveFound)
        {
            selectedPiece = piece;
            board[piece.Row_ID, piece.Coloum_ID].HighlightPieceBlock();
            highlightedBlocks.Add(board[piece.Row_ID, piece.Coloum_ID]);

        }
        else
        {
            selectedPiece = null;
            CheckMoves();
        }
    }

    public void MovePiece(Block block)
    {
        ResetHighlightedBlocks(selectedPiece.PieceType);

        if(block.IsNextToNextHighlighted)
        {
            Debug.Log("Jumped");
        }

        board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].IsPiecePresent = false;
        board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].Piece = null;

        block.IsPiecePresent = true;
        block.Piece = selectedPiece;

        selectedPiece.Row_ID = block.Row_ID;
        selectedPiece.Coloum_ID = block.Coloum_ID;
        selectedPiece.transform.position = block.transform.position;

        GameManager.instance.ChangeTurn();
    }
}
