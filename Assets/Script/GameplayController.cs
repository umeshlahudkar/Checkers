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
        ResetHighlightedBlocks();

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
                        && row + 2 < 8 && coloum + 2 < 8 && !board[row + 2, coloum + 2].IsPiecePresent)
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
                         && row - 2 >= 0 && coloum - 2 >= 0 && !board[row - 2, coloum - 2].IsPiecePresent)
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
                         && row - 2 >= 0 && coloum + 2 < 8 && !board[row - 2, coloum + 2].IsPiecePresent)
                {
                    pieceBlock.HighlightPieceBlock();
                    highlightedBlocks.Add(pieceBlock);
                    blockFound = true;
                }
            }
        }
        return blockFound;
    }

    private void ResetHighlightedBlocks()
    {
        foreach (Block b in highlightedBlocks)
        {
            b.ResetBlock();
        }
        highlightedBlocks.Clear();
    }

    public bool CheckNextMove(Piece piece)
    {
        ResetHighlightedBlocks();

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
                         && row + 2 < 8 && coloum + 2 < 8 && !board[row + 2, coloum + 2].IsPiecePresent)
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
                        && row - 2 >= 0 && coloum - 2 >= 0 && !board[row - 2, coloum - 2].IsPiecePresent)
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
                       && row - 2 >= 0 && coloum + 2 < 8 && !board[row - 2, coloum + 2].IsPiecePresent)
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

        return moveFound;
    }

    public void MovePiece(Block block)
    {
        ResetHighlightedBlocks();

        if(block.IsNextToNextHighlighted)
        {
            int row = block.Row_ID;
            int coloum = block.Coloum_ID;
     
            if (selectedPiece.PieceType == PieceType.White)
            {
                bool onLeftSide = block.Coloum_ID < selectedPiece.Coloum_ID ? true : false;
                
                if(onLeftSide)
                {
                    Piece piece = board[row - 1, coloum + 1].Piece;
                    if (blackPieces.Contains(piece))
                    {
                        blackPieces.Remove(piece);
                    }

                    Destroy(piece.gameObject);
                    board[row - 1, coloum + 1].Piece = null;
                    board[row - 1, coloum + 1].IsPiecePresent = false;
                }
                else
                {
                    Piece piece = board[row - 1, coloum - 1].Piece;
                    if (blackPieces.Contains(piece))
                    {
                        blackPieces.Remove(piece);
                    }

                    Destroy(piece.gameObject);
                    board[row - 1, coloum - 1].Piece = null;
                    board[row - 1, coloum - 1].IsPiecePresent = false;
                }
            }
            else if (selectedPiece.PieceType == PieceType.Black)
            {
                bool onLeftSide = block.Coloum_ID < selectedPiece.Coloum_ID ? true : false;

                if (onLeftSide)
                {
                    Piece piece = board[row + 1, coloum + 1].Piece;
                    if (whitePieces.Contains(piece))
                    {
                        whitePieces.Remove(piece);
                    }

                    Destroy(piece.gameObject);
                    board[row + 1, coloum + 1].Piece = null;
                    board[row + 1, coloum + 1].IsPiecePresent = false;
                }
                else
                {
                    Piece piece = board[row + 1, coloum - 1].Piece;
                    if (whitePieces.Contains(piece))
                    {
                        whitePieces.Remove(piece);
                    }

                    Destroy(piece.gameObject);
                    board[row + 1, coloum - 1].Piece = null;
                    board[row + 1, coloum - 1].IsPiecePresent = false;
                }
            }
        }

        board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].IsPiecePresent = false;
        board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].Piece = null;

        block.IsPiecePresent = true;
        block.Piece = selectedPiece;

        selectedPiece.Row_ID = block.Row_ID;
        selectedPiece.Coloum_ID = block.Coloum_ID;
        selectedPiece.transform.position = block.transform.position;

        selectedPiece = block.Piece;

        if(CanMove())
        {
            CheckNextMove(selectedPiece);
        }
        else
        {
            GameManager.instance.ChangeTurn();
        }
    }

    private bool CanMove()
    {
        if(selectedPiece.PieceType == PieceType.White)
        {
            int row = selectedPiece.Row_ID;
            int coloum = selectedPiece.Coloum_ID;

            if(row + 1 < 8 && coloum - 1 >= 0 && board[row + 1, coloum - 1].IsPiecePresent && selectedPiece.PieceType != board[row + 1, coloum - 1].Piece.PieceType &&
               row + 2 < 8 && coloum - 2 >= 0 && !board[row + 2, coloum - 2].IsPiecePresent)
            {
                return true;
            }

            if (row + 1 < 8 && coloum + 1 < 8 && board[row + 1, coloum + 1].IsPiecePresent && selectedPiece.PieceType != board[row + 1, coloum + 1].Piece.PieceType &&
              row + 2 < 8 && coloum + 2 < 8 && !board[row + 2, coloum + 2].IsPiecePresent)
            {
                return true;
            }
        }
        else if (selectedPiece.PieceType == PieceType.Black)
        {
            int row = selectedPiece.Row_ID;
            int coloum = selectedPiece.Coloum_ID;

            if (row - 1 >= 0 && coloum - 1 >= 0 && board[row - 1, coloum - 1].IsPiecePresent && selectedPiece.PieceType != board[row - 1, coloum - 1].Piece.PieceType &&
               row - 2 >= 0 && coloum - 2 >= 0 && !board[row -2, coloum - 2].IsPiecePresent)
            {
                return true;
            }

            if (row - 1 >= 0 && coloum + 1 < 8 && board[row - 1, coloum + 1].IsPiecePresent && selectedPiece.PieceType != board[row - 1, coloum + 1].Piece.PieceType &&
              row - 2 >= 0 && coloum + 2 < 8 && !board[row - 2, coloum + 2].IsPiecePresent)
            {
                return true;
            }
        }
        return false;
    }
}
