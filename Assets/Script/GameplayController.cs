using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

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

    [SerializeField] private List<Block> highlightedBlocks = new List<Block>();

    public Piece selectedPiece;


    public void OnBoardReady()
    {
        CheckMoves();
    }

    public bool CheckMoves()
    {
        ResetHighlightedBlocks();

        if (GameManager.instance.PieceType == PieceType.Black)
        {
            return CheckBlackPieceMove();
        }
        else if (GameManager.instance.PieceType == PieceType.White)
        {
            return CheckWhitePieceMove();
        }

        return false;
    }

    public bool CheckWhitePieceMove()
    {
        bool blockFound = false;
        foreach (Piece piece in whitePieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            Block pieceBlock = board[row, coloum];

            // diagonally down left
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

            //diagonally down right
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

            if(piece.IsCrownedKing)
            {
                // diagonally up left
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

                //diagonally up right
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

            // diagonally up left
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

            // diagonally up right
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

            if (piece.IsCrownedKing)
            {
                // diagonally down left
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

                //diagonally down right
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
            // diagonally down left
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

            // diagonally down right
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

            if (piece.IsCrownedKing)
            {
                // diagonally up left
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

                //diagonally up right
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
        }
        else if (piece.PieceType == PieceType.Black)
        {
            // diagonally up left
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

            // diagonally up right
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

            if (piece.IsCrownedKing)
            {
                // diagonally down left
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

                //diagonally down right
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

        bool hasDeleted = false;

        if(block.IsNextToNextHighlighted)
        {
            int row = block.Row_ID;
            int coloum = block.Coloum_ID;

            int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
            int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

            Piece piece = board[targetRow, targetCol].Piece;
            //if (blackPieces.Contains(piece))
            {
                //blackPieces.Remove(piece);
            }
            //PhotonNetwork.Destroy(piece.gameObject);
            piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
            //board[targetRow, targetCol].SetBlockPiece(false, null);

           // GameManager.instance.UpdateGrid(targetRow, targetCol, null);
            //if (selectedPiece.PieceType == PieceType.White)
            //{
            //    bool onLeftSide = coloum < selectedPiece.Coloum_ID;
               

            //    if (onLeftSide)
            //    {
            //        bool onDownSide = row > selectedPiece.Row_ID;

            //        if (onDownSide)
            //        {
            //            Piece piece = board[row - 1, coloum + 1].Piece;
            //            if (blackPieces.Contains(piece))
            //            {
            //                blackPieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row - 1, coloum + 1].Piece = null;
            //            //board[row - 1, coloum + 1].IsPiecePresent = false;

            //            board[row - 1, coloum + 1].SetBlockPiece(false, null);
            //        }
            //        else
            //        {
            //            Piece piece = board[row + 1, coloum + 1].Piece;
            //            if (blackPieces.Contains(piece))
            //            {
            //                blackPieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row + 1, coloum + 1].Piece = null;
            //            //board[row + 1, coloum + 1].IsPiecePresent = false;

            //            board[row + 1, coloum + 1].SetBlockPiece(false, null);
            //        }
            //    }
            //    else
            //    {
            //        bool onDownSide = block.Row_ID > selectedPiece.Row_ID;

            //        if(onDownSide)
            //        {
            //            Piece piece = board[row - 1, coloum - 1].Piece;
            //            if (blackPieces.Contains(piece))
            //            {
            //                blackPieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row - 1, coloum - 1].Piece = null;
            //            //board[row - 1, coloum - 1].IsPiecePresent = false;

            //            board[row - 1, coloum - 1].SetBlockPiece(false, null);
            //        }
            //        else
            //        {
            //            Piece piece = board[row + 1, coloum - 1].Piece;
            //            if (blackPieces.Contains(piece))
            //            {
            //                blackPieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row + 1, coloum - 1].Piece = null;
            //            //board[row + 1, coloum - 1].IsPiecePresent = false;

            //            board[row + 1, coloum - 1].SetBlockPiece(false, null);
            //        }
            //    }
                
            //}
            //else if (selectedPiece.PieceType == PieceType.Black)
            //{
            //    bool onLeftSide = block.Coloum_ID < selectedPiece.Coloum_ID;

               
            //    if (onLeftSide)
            //    {
            //        bool onDownSide = block.Row_ID > selectedPiece.Row_ID;

            //        if(onDownSide)
            //        {
            //            Piece piece = board[row - 1, coloum + 1].Piece;
            //            if (whitePieces.Contains(piece))
            //            {
            //                whitePieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row - 1, coloum + 1].Piece = null;
            //            //board[row - 1, coloum + 1].IsPiecePresent = false;

            //            board[row - 1, coloum + 1].SetBlockPiece(false, null);
            //        }
            //        else
            //        {
            //            Piece piece = board[row + 1, coloum + 1].Piece;
            //            if (whitePieces.Contains(piece))
            //            {
            //                whitePieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row + 1, coloum + 1].Piece = null;
            //            //board[row + 1, coloum + 1].IsPiecePresent = false;

            //            board[row + 1, coloum + 1].SetBlockPiece(false, null);
            //        }
            //    }
            //    else
            //    {
            //        bool onDownSide = block.Row_ID > selectedPiece.Row_ID;

            //        if(onDownSide)
            //        {
            //            Piece piece = board[row - 1, coloum - 1].Piece;
            //            if (whitePieces.Contains(piece))
            //            {
            //                whitePieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row - 1, coloum - 1].Piece = null;
            //            //board[row - 1, coloum - 1].IsPiecePresent = false;

            //            board[row - 1, coloum - 1].SetBlockPiece(false, null);
            //        }
            //        else
            //        {
            //            Piece piece = board[row + 1, coloum - 1].Piece;
            //            if (whitePieces.Contains(piece))
            //            {
            //                whitePieces.Remove(piece);
            //            }

            //            Destroy(piece.gameObject);
            //            //board[row + 1, coloum - 1].Piece = null;
            //            //board[row + 1, coloum - 1].IsPiecePresent = false;

            //            board[row + 1, coloum - 1].SetBlockPiece(false, null);
            //        }
            //    }
            //}
            hasDeleted = true;
        }

        //board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].IsPiecePresent = false;
        //board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].Piece = null;

        //board[selectedPiece.Row_ID, selectedPiece.Coloum_ID].SetBlockPiece(false, null);
        GameManager.instance.UpdateGrid(selectedPiece.Row_ID, selectedPiece.Coloum_ID, null);

        //block.IsPiecePresent = true;
        //block.Piece = selectedPiece;

        //block.SetBlockPiece(true, selectedPiece);
        GameManager.instance.UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

        //selectedPiece.Row_ID = block.Row_ID;
        //selectedPiece.Coloum_ID = block.Coloum_ID;
        selectedPiece.transform.position = block.transform.position;

        if ((selectedPiece.PieceType == PieceType.White && selectedPiece.Row_ID == 7) ||
            (selectedPiece.PieceType == PieceType.Black && selectedPiece.Row_ID == 0))
        {
            //selectedPiece.SetCrownKing();
            selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
        }

        selectedPiece = block.Piece;

        if (hasDeleted && CanMove())
        {
            CheckNextMove(selectedPiece);
        }
        else
        {
            GameManager.instance.SwitchTurn();
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
