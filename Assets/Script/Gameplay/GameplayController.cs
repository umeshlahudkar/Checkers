using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameplayController : Singleton<GameplayController>
{
    [SerializeField] private PhotonView thisPhotonView;

    public Block[,] board = new Block[8, 8];
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

    [SerializeField] private List<Block> highlightedBlocks = new List<Block>();

    public Piece selectedPiece;
    public AIPlayer aiPlayer;

    public void OnBoardReady()
    {
        CheckMoves();
    }

    public bool CheckMoves(bool canHighlightMoves = true)
    {
        ResetHighlightedBlocks();

        if (GameManager.Instance.PieceType == PieceType.Black)
        {
            return CheckAllBlackPieceMove(canHighlightMoves);
        }
        else if (GameManager.Instance.PieceType == PieceType.White)
        {
            return CheckAllWhitePieceMove(canHighlightMoves);
        }

        return false;
    }

    #region AI
    //AI

    public bool CanMove(int playerNumber)
    {
        if (playerNumber == 1)
        {
            for(int i = 0; i < blackPieces.Count; i++)
            {
                bool moveFound = CanPieceMove(blackPieces[i]);
                if(moveFound)
                {
                    return moveFound;
                }
            }
        }
        else if (playerNumber == 2)
        {
            for (int i = 0; i < whitePieces.Count; i++)
            {
                bool moveFound = CanPieceMove(whitePieces[i]);
                if (moveFound)
                {
                    return moveFound;
                }
            }
        }

        return false;
    }


    //AI
    private bool CheckPieceCanMove(int playerID, int row, int col, int jumpRow, int jumpCol)
    {
        if (!IsValidPosition(row, col)) { return false; }

        bool moveFound = false;

        if ((!board[row, col].IsPiecePresent) ||
            (board[row, col].IsPiecePresent && playerID != board[row, col].Piece.Player_ID &&
                IsValidPosition(jumpRow, jumpCol) && !board[jumpRow, jumpCol].IsPiecePresent))
        {
            moveFound = true;
        }
        return moveFound;
    }

    public bool CanPieceMove(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;
        int playerID = piece.Player_ID;

        bool moveFound = false;

        if (playerID == 2)
        {
            // diagonally down left
            moveFound |= CheckPieceCanMove(playerID, row + 1, coloum - 1, row + 2, coloum - 2);
            //diagonally down right
            moveFound |= CheckPieceCanMove(playerID, row + 1, coloum + 1, row + 2, coloum + 2);

            if (piece.IsCrownedKing)
            {
                // diagonally up left
                moveFound |= CheckPieceCanMove(playerID, row - 1, coloum - 1, row - 2, coloum - 2);
                //diagonally up right
                moveFound |= CheckPieceCanMove(playerID, row - 1, coloum + 1, row - 2, coloum + 2);
            }
        }
        else if (playerID == 1)
        {
            // diagonally up left
            moveFound |= CheckPieceCanMove(playerID, row - 1, coloum - 1, row - 2, coloum - 2);
            // diagonally up right
            moveFound |= CheckPieceCanMove(playerID, row - 1, coloum + 1, row - 2, coloum + 2);

            if (piece.IsCrownedKing)
            {
                // diagonally down left
                moveFound |= CheckPieceCanMove(playerID, row + 1, coloum - 1, row + 2, coloum - 2);
                //diagonally down right
                moveFound |= CheckPieceCanMove(playerID, row + 1, coloum + 1, row + 2, coloum + 2);
            }
        }
        return moveFound;
    }

    public bool CanPieceKill(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;

        bool canKill = false;

        if (piece.Player_ID == 2)
        {
            // diagonally down left
            canKill |= CanKillAdjecentPiece(piece, row + 2, coloum - 2);
            //diagonally down right
            canKill |= CanKillAdjecentPiece(piece, row + 2, coloum + 2);

            if (piece.IsCrownedKing)
            {
                // diagonally up left
                canKill |= CanKillAdjecentPiece(piece, row - 2, coloum - 2);
                //diagonally up right
                canKill |= CanKillAdjecentPiece(piece, row - 2, coloum + 2);
            }
        }
        else if (piece.Player_ID == 1)
        {
            // diagonally up left
            canKill |= CanKillAdjecentPiece(piece, row - 2, coloum - 2);
            // diagonally up right
            canKill |= CanKillAdjecentPiece(piece, row - 2, coloum + 2);

            if (piece.IsCrownedKing)
            {
                // diagonally down left
                canKill |= CanKillAdjecentPiece(piece, row + 2, coloum - 2);
                //diagonally down right
                canKill |= CanKillAdjecentPiece(piece, row + 2, coloum + 2);
            }
        }
        return canKill;
    }

    //AI 
    public void CheckMovablePieces(int playerID, List<Piece> movablePieces)
    {
        if (playerID == 1)
        {
            for (int i = 0; i < blackPieces.Count; i++)
            {
                bool moveFound = CanPieceMove(blackPieces[i]);
                if (moveFound)
                {
                    movablePieces.Add(blackPieces[i]);
                }
            }
        }
        else if (playerID == 2)
        {
            for (int i = 0; i < whitePieces.Count; i++)
            {
                bool moveFound = CanPieceMove(whitePieces[i]);
                if (moveFound)
                {
                    movablePieces.Add(whitePieces[i]);
                }
            }
        }
    }

    //AI 
    private bool CanMoveAtAdjacentBlock(int row, int col)
    {
        if(!IsValidPosition(row, col)) { return false; }

        if(!board[row, col].IsPiecePresent)
        {
            return true;
        }
        return false;
    }

    public void SetPiecePosition(Piece piece)
    {
        SetAdjacentMovePosition(piece);
        SetAdjacentKillPosition(piece);
        SetDoubleKillPosition(piece);
    }

    public void SetAdjacentKillPosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        if(piece.Player_ID == 2)
        {
            // diagonal down left
            CheckDiagonalAdjacentKill(piece, row + 2, col - 2);

            // diagonal down right
            CheckDiagonalAdjacentKill(piece, row + 2, col + 2);

            if (piece.IsCrownedKing)
            {
                // diagonal up left
                CheckDiagonalAdjacentKill(piece, row - 2, col - 2);

                // diagonal up right
                CheckDiagonalAdjacentKill(piece, row - 2, col + 2);
            }
        }
        else if (piece.Player_ID == 1)
        {
            // diagonal up left
            CheckDiagonalAdjacentKill(piece, row - 2, col - 2);

            // diagonal up right
            CheckDiagonalAdjacentKill(piece, row - 2, col + 2);

            if (piece.IsCrownedKing)
            {
                // diagonal down left
                CheckDiagonalAdjacentKill(piece, row + 2, col - 2);

                // diagonal down right
                CheckDiagonalAdjacentKill(piece, row + 2, col + 2);
            }
        }
    }

    private void SetAdjacentMovePosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        if (piece.Player_ID == 2)
        {
            // diagonal down left
            CheckDiagonalAdjacentMove(piece, row + 1, col - 1);

            // diagonal down right
            CheckDiagonalAdjacentMove(piece, row + 1, col + 1);

            if (piece.IsCrownedKing)
            {
                // diagonal up left
                CheckDiagonalAdjacentMove(piece, row - 1, col - 1);

                // diagonal up right
                CheckDiagonalAdjacentMove(piece, row - 1, col + 1);
            }
        }
        else if (piece.Player_ID == 1)
        {
            // diagonal up left
            CheckDiagonalAdjacentMove(piece, row - 1, col - 1);

            // diagonal up right
            CheckDiagonalAdjacentMove(piece, row - 1, col + 1);

            if (piece.IsCrownedKing)
            {
                // diagonal down left
                CheckDiagonalAdjacentMove(piece, row + 1, col - 1);

                // diagonal down right
                CheckDiagonalAdjacentMove(piece, row + 1, col + 1);
            }
        }
    }

    private void SetDoubleKillPosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        if (piece.Player_ID == 2)
        {
            // diagonal down left
            CheckForDoubleKill(piece, row + 2, col - 2);

            // diagonal down right
            CheckForDoubleKill(piece, row + 2, col + 2);

            if(piece.IsCrownedKing)
            {
                // diagonal up left
                CheckForDoubleKill(piece, row - 2, col - 2);

                // diagonal up right
                CheckForDoubleKill(piece, row - 2, col + 2);
            }
        }
        else if (piece.Player_ID == 1)
        {
            // diagonal up left
            CheckForDoubleKill(piece, row - 2, col - 2);

            // diagonal up right
            CheckForDoubleKill(piece, row - 2, col + 2);

            if (piece.IsCrownedKing)
            {
                // diagonal down left
                CheckForDoubleKill(piece, row + 2, col - 2);

                // diagonal down right
                CheckForDoubleKill(piece, row + 2, col + 2);
            }
        }
    }

    private void CheckDiagonalAdjacentMove(Piece piece, int targetRow, int targetCol)
    {
        if (CanMoveAtAdjacentBlock(targetRow, targetCol))
        {
            if (IsSafeToMove(piece, targetRow, targetCol))
            {
                piece.safeMovableBlockPositions.Add(new BoardPosition(targetRow, targetCol));
            }
            else
            {
                piece.movableBlockPositions.Add(new BoardPosition(targetRow, targetCol));
            }
        }
    }

    private void CheckDiagonalAdjacentKill(Piece piece, int targetRow, int targetCol)
    {
        if (CanKillAdjecentPiece(piece, targetRow, targetCol))
        {
            if (IsSafeToKill(piece, targetRow, targetCol))
            {
                piece.safeKillerBlockPositions.Add(new BoardPosition(targetRow, targetCol));
            }
            else
            {
                piece.killerBlockPositions.Add(new BoardPosition(targetRow, targetCol));
            }
        }
    }

    private bool CanKillAdjecentPiece(Piece killerPiece, int targetRow, int targetCol)
    {
        int middleBlockRow = (targetRow > killerPiece.Row_ID) ? targetRow - 1 : targetRow + 1;
        int middleBlockCol = (targetCol > killerPiece.Coloum_ID) ? targetCol - 1 : targetCol + 1;
       
        if (!IsValidPosition(middleBlockRow, middleBlockCol) || !IsValidPosition(targetRow, targetCol)) { return false; }

        if (board[middleBlockRow, middleBlockCol].IsPiecePresent && killerPiece.Player_ID != board[middleBlockRow, middleBlockCol].Piece.Player_ID &&
            !board[targetRow, targetCol].IsPiecePresent)
        {
            return true;
        }
        return false;
    }

    private bool CanDoubleKill(Piece killerPiece, int targetRow, int targetCol)
    {
        if(!CanKillAdjecentPiece(killerPiece, targetRow, targetCol) || !IsValidPosition(targetRow, targetCol)) { return false; }

        int killerRow = killerPiece.Row_ID;
        int killerCol = killerPiece.Coloum_ID;

        int middleBlockRow = (targetRow > killerPiece.Row_ID) ? targetRow - 1 : targetRow + 1;
        int middleBlockCol = (targetCol > killerPiece.Coloum_ID) ? targetCol - 1 : targetCol + 1;

        Piece firstKilledPiece = board[middleBlockRow, middleBlockCol].Piece;

        board[middleBlockRow, middleBlockCol].SetBlockPiece(false, null);
        board[killerRow, killerCol].SetBlockPiece(false, null);

        board[targetRow, targetCol].SetBlockPiece(true, killerPiece);

        bool canKill = false;

        // diagonal down left
        canKill |= CanKillAdjecentPiece(killerPiece, targetRow + 2, targetCol - 2);
        // diagonal down right
        canKill |= CanKillAdjecentPiece(killerPiece, targetRow + 2, targetCol + 2);

        if(killerPiece.IsCrownedKing)
        {
            // diagonal up left
            canKill |= CanKillAdjecentPiece(killerPiece, targetRow - 2, targetCol - 2);
            // diagonal up right
            canKill |= CanKillAdjecentPiece(killerPiece, targetRow - 2, targetCol + 2);
        }

        board[middleBlockRow, middleBlockCol].SetBlockPiece(true, firstKilledPiece);
        board[killerRow, killerCol].SetBlockPiece(true, killerPiece);
        board[targetRow, targetCol].SetBlockPiece(false, null);

        return canKill;
    }

    private bool IsSafeToMove(Piece piece, int targetRow, int targetCol)
    {
        int initialPieceRow = piece.Row_ID;
        int initialPieceCol = piece.Coloum_ID;

        board[initialPieceRow, initialPieceCol].SetBlockPiece(false, null);
        board[targetRow, targetCol].SetBlockPiece(true, piece);

        bool isSafe = IsSafe(piece);

        board[targetRow, targetCol].SetBlockPiece(false, null);
        board[initialPieceRow, initialPieceCol].SetBlockPiece(true, piece);

        return isSafe;
    }

    private bool IsSafeToKill(Piece killerPiece, int targetRow, int targetCol)
    {
        if(!IsValidPosition(targetRow, targetCol)) { return false; }

        int killerRow = killerPiece.Row_ID;
        int killerCol = killerPiece.Coloum_ID;

        int middleBlockRow = (targetRow > killerRow) ? targetRow - 1 : targetRow + 1;
        int middleBlockCol = (targetCol > killerCol) ? targetCol - 1 : targetCol + 1;

        Piece gettingKilled = board[middleBlockRow, middleBlockCol].Piece;

        board[middleBlockRow, middleBlockCol].SetBlockPiece(false, null);
        board[targetRow, targetCol].SetBlockPiece(true, killerPiece);

        bool isSafe = IsSafe(killerPiece);

        board[targetRow, targetCol].SetBlockPiece(false, null);
        board[middleBlockRow, middleBlockCol].SetBlockPiece(true, gettingKilled);

        killerPiece.Row_ID = killerRow;
        killerPiece.Coloum_ID = killerCol;

        return isSafe;
    }

    private void CheckForDoubleKill(Piece killerPiece, int targetRow, int targetCol)
    {
        if (!IsValidPosition(targetRow, targetCol)) { return ; }

        if(!CanDoubleKill(killerPiece, targetRow, targetCol)) { return; }

        int killerRow = killerPiece.Row_ID;
        int killerCol = killerPiece.Coloum_ID;

        int middleBlockRow = (targetRow > killerPiece.Row_ID) ? targetRow - 1 : targetRow + 1;
        int middleBlockCol = (targetCol > killerPiece.Coloum_ID) ? targetCol - 1 : targetCol + 1;

        Piece firstKilledPiece = board[middleBlockRow, middleBlockCol].Piece;

        board[middleBlockRow, middleBlockCol].SetBlockPiece(false, null);
        board[killerRow, killerCol].SetBlockPiece(false, null);
        board[targetRow, targetCol].SetBlockPiece(true, killerPiece);

        // diagonal down left
        CheckDoubleKillDirection(killerPiece, targetRow + 2, targetCol - 2);
        // diagonal down right
        CheckDoubleKillDirection(killerPiece, targetRow + 2, targetCol + 2);

        if (killerPiece.IsCrownedKing)
        {
            // diagonal up left
            CheckDoubleKillDirection(killerPiece, targetRow - 2, targetCol - 2);
            // diagonal up right
            CheckDoubleKillDirection(killerPiece, targetRow - 2, targetCol + 2);
        }

        board[middleBlockRow, middleBlockCol].SetBlockPiece(true, firstKilledPiece);
        board[killerRow, killerCol].SetBlockPiece(true, killerPiece);
        board[targetRow, targetCol].SetBlockPiece(false, null);
    }

    private void CheckDoubleKillDirection(Piece killerPiece, int targetRow, int targetCol)
    {
        if (CanKillAdjecentPiece(killerPiece, targetRow, targetCol))
        {
            int rowToAdd = (targetRow > killerPiece.Row_ID) ? targetRow - 2 : targetRow + 2;
            int colToAdd = (targetCol > killerPiece.Coloum_ID) ? targetCol - 2 : targetCol + 2;

            if (IsSafeToKill(killerPiece, targetRow, targetCol))
            {
                killerPiece.safeDoubleKillerBlockPositions.Add(new BoardPosition(rowToAdd, colToAdd));
            }
            else
            {
                killerPiece.doubleKillerBlockPositions.Add(new BoardPosition(rowToAdd, colToAdd));
            }
        }
    }

    private bool IsSafe(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;
        int playerID = piece.Player_ID;

        if (col == 0 || col == 7 || row == 0 || row == 7) 
        {
            /* safe position */
            return true;
        }

        if (playerID == 2)
        {
            // diagonal down left
            if (IsValidPosition(row + 1, col - 1) && board[row + 1, col - 1].IsPiecePresent && board[row + 1, col - 1].Piece.Player_ID != playerID &&
               IsValidPosition(row - 1, col + 1) && !board[row - 1, col + 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal down right
            if (IsValidPosition(row + 1, col + 1) && board[row + 1, col + 1].IsPiecePresent && board[row + 1, col + 1].Piece.Player_ID != playerID &&
              IsValidPosition(row - 1, col - 1) && !board[row - 1, col - 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal up left
            if (IsValidPosition(row - 1, col - 1) && board[row - 1, col - 1].IsPiecePresent && board[row - 1, col - 1].Piece.Player_ID != playerID &&
               board[row - 1, col - 1].Piece.IsCrownedKing && IsValidPosition(row + 1, col + 1) && !board[row + 1, col + 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal up right
            if (IsValidPosition(row - 1, col + 1) && board[row - 1, col + 1].IsPiecePresent && board[row - 1, col + 1].Piece.Player_ID != playerID &&
              board[row - 1, col + 1].Piece.IsCrownedKing && IsValidPosition(row + 1, col - 1) && !board[row + 1, col - 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }
        }
        else if(playerID == 1)
        {
            // diagonal up left
            if (IsValidPosition(row - 1, col - 1) && board[row - 1, col - 1].IsPiecePresent && board[row - 1, col - 1].Piece.Player_ID != playerID &&
              IsValidPosition(row + 1, col + 1) && !board[row + 1, col + 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal up right
            if (IsValidPosition(row - 1, col + 1) && board[row - 1, col + 1].IsPiecePresent && board[row - 1, col + 1].Piece.Player_ID != playerID &&
              IsValidPosition(row + 1, col - 1) && !board[row + 1, col - 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal down left
            if (IsValidPosition(row + 1, col - 1) && board[row + 1, col - 1].IsPiecePresent && board[row + 1, col - 1].Piece.Player_ID != playerID &&
               board[row + 1, col - 1].Piece.IsCrownedKing && IsValidPosition(row - 1, col + 1) && !board[row - 1, col + 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }

            // diagonal down right
            if (IsValidPosition(row + 1, col + 1) && board[row + 1, col + 1].IsPiecePresent && board[row + 1, col + 1].Piece.Player_ID != playerID &&
              board[row + 1, col + 1].Piece.IsCrownedKing && IsValidPosition(row - 1, col - 1) && !board[row - 1, col - 1].IsPiecePresent)
            {
                /* not safe position */
                return false;
            }
        }
        return true;
    }

    #endregion

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
    }

    private bool CheckPieceMoveAndHighlightBlock(int row, int col, int jumpRow, int jumpCol, Piece piece, Block block, bool canHighlightMoves)
    {
        if(!IsValidPosition(row, col)) { return false; }

        bool moveFound = false;

        if((!board[row, col].IsPiecePresent) || 
            (board[row, col].IsPiecePresent && piece.PieceType != board[row, col].Piece.PieceType &&
                IsValidPosition(jumpRow, jumpCol) && !board[jumpRow, jumpCol].IsPiecePresent))
        {
            moveFound = true;
        }

        if(moveFound && canHighlightMoves)
        {
            block.HighlightPieceBlock();
            highlightedBlocks.Add(block);
        }
        return moveFound;
    }

    private bool CheckAllWhitePieceMove(bool canHighlightMoves)
    {
        bool moveFound = false;
        foreach (Piece piece in whitePieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;
            Block pieceBlock = board[row, coloum];

            // diagonally down left
            moveFound |= CheckPieceMoveAndHighlightBlock(row + 1, coloum - 1, row + 2, coloum - 2, piece, pieceBlock, canHighlightMoves);
            //diagonally down right
            moveFound |= CheckPieceMoveAndHighlightBlock(row + 1, coloum + 1, row + 2, coloum + 2, piece, pieceBlock, canHighlightMoves);

            if(piece.IsCrownedKing)
            {
                // diagonally up left
                moveFound |= CheckPieceMoveAndHighlightBlock(row - 1, coloum - 1, row - 2, coloum - 2, piece, pieceBlock, canHighlightMoves);
                //diagonally up right
                moveFound |= CheckPieceMoveAndHighlightBlock(row - 1, coloum + 1, row - 2, coloum + 2, piece, pieceBlock, canHighlightMoves);
            }
        }
        return moveFound;
    }

    public bool CheckAllBlackPieceMove(bool canHighlightMoves)
    {
        bool moveFound = false;
        foreach (Piece piece in blackPieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            Block pieceBlock = board[row, coloum];

            // diagonally up left
            moveFound |= CheckPieceMoveAndHighlightBlock(row - 1, coloum - 1, row - 2, coloum - 2, piece, pieceBlock, canHighlightMoves);
            // diagonally up right
            moveFound |= CheckPieceMoveAndHighlightBlock(row - 1, coloum + 1, row - 2, coloum + 2, piece, pieceBlock, canHighlightMoves);

            if (piece.IsCrownedKing)
            {
                // diagonally down left
                moveFound |= CheckPieceMoveAndHighlightBlock(row + 1, coloum - 1, row + 2, coloum - 2, piece, pieceBlock, canHighlightMoves);
                //diagonally down right
                moveFound |= CheckPieceMoveAndHighlightBlock(row + 1, coloum + 1, row + 2, coloum + 2, piece, pieceBlock, canHighlightMoves);
            }
        }
        return moveFound;
    }

    private void ResetHighlightedBlocks()
    {
        foreach (Block b in highlightedBlocks)
        {
            b.ResetBlock();
        }
        highlightedBlocks.Clear();
    }

    private bool CheckPieceNextMoveAndHighlightTargetBlock(int targetRow, int targetCol, int jumpTargetRow, int jumpTargetCol, Piece pieceToMove)
    {
        if(!IsValidPosition(targetRow, targetCol)) { return false; }

        bool targetMoveFound = false;

        if (!board[targetRow, targetCol].IsPiecePresent)
        {
            board[targetRow, targetCol].HighlightNextMoveBlock();
            highlightedBlocks.Add(board[targetRow, targetCol]);
            targetMoveFound = true;
        } 
        else if(board[targetRow, targetCol].IsPiecePresent && pieceToMove.PieceType != board[targetRow, targetCol].Piece.PieceType
             && IsValidPosition(jumpTargetRow, jumpTargetCol) && !board[jumpTargetRow, jumpTargetCol].IsPiecePresent)
        {
            board[jumpTargetRow, jumpTargetCol].HighlightNextMoveBlock(true);
            highlightedBlocks.Add(board[jumpTargetRow, jumpTargetCol]);
            targetMoveFound = true;
        }
        return targetMoveFound;
    }

    public bool CheckNextMove(Piece pieceToMove)
    {
        ResetHighlightedBlocks();

        int row = pieceToMove.Row_ID;
        int coloum = pieceToMove.Coloum_ID;
        bool moveFound = false;

        if (pieceToMove.PieceType == PieceType.White)
        {
            // diagonally down left
            moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceToMove);
            // diagonally down right
            moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceToMove);

            if(pieceToMove.IsCrownedKing)
            {
                // diagonally up left
                moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceToMove);
                //diagonally up right
                moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceToMove);
            }
        }
        else if (pieceToMove.PieceType == PieceType.Black)
        {
            // diagonally up left
            moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceToMove);
            // diagonally up right
            moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceToMove);

            if (pieceToMove.IsCrownedKing)
            {
                // diagonally down left
                moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceToMove);
                //diagonally down right
                moveFound |= CheckPieceNextMoveAndHighlightTargetBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceToMove);
            }
        }

        if (moveFound)
        {
            selectedPiece = pieceToMove;
            board[pieceToMove.Row_ID, pieceToMove.Coloum_ID].HighlightPieceBlock();
            highlightedBlocks.Add(board[pieceToMove.Row_ID, pieceToMove.Coloum_ID]);
        }
        else
        {
            selectedPiece = null;
            CheckMoves();
        }

        return moveFound;
    }

    public IEnumerator HandlePieceMovement(Block block)
    {
        GameManager.Instance.ResetTurnMissCount();
        ResetHighlightedBlocks();

        bool hasDeleted = false;

        if(block.IsNextToNextHighlighted)
        {
            int row = block.Row_ID;
            int coloum = block.Coloum_ID;

            int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
            int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

            Piece piece = board[targetRow, targetCol].Piece;
            if(GameManager.Instance.GameMode == GameMode.Online)
            {
                piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
            }
            else
            {
                piece.Destroy();
            }
            hasDeleted = true;
            block.IsNextToNextHighlighted = false;
        }

        UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

        yield return new WaitForSeconds(0.5f);  //0.25

        GameplayUIController.Instance.StopPlayerHighlightAnim();

        if (!selectedPiece.IsCrownedKing && ((selectedPiece.PieceType == PieceType.White && selectedPiece.Row_ID == 7) ||
            (selectedPiece.PieceType == PieceType.Black && selectedPiece.Row_ID == 0)))
        {
            if(GameManager.Instance.GameMode == GameMode.Online)
            {
                selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
            }
            else
            {
                selectedPiece.SetCrownKing();
            }
        }

        selectedPiece = block.Piece;

        if (hasDeleted && CanPieceKill(selectedPiece) /*CanMove()*/)
        {
            if(GameManager.Instance.GameMode == GameMode.PVC && GameManager.Instance.CurrentTurn == 2)
            {
                selectedPiece.ResetAllList();
                SetAdjacentKillPosition(selectedPiece);
                BoardPosition position = default;

                if (selectedPiece.safeKillerBlockPositions.Count > 0)
                {
                    position = selectedPiece.safeKillerBlockPositions[0];
                }
                else if (selectedPiece.killerBlockPositions.Count > 0)
                {
                    position = selectedPiece.killerBlockPositions[0];
                }

                board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                aiPlayer.MovePiece(selectedPiece, position);
                ResetBlocksNextToNextHighlightedParameter();
            }
            else
            {
                CheckNextMove(selectedPiece);
            }
        }
        else
        {
            GameManager.Instance.SwitchTurn();
            ResetBlocksNextToNextHighlightedParameter();
        }
    }

    private void ResetBlocksNextToNextHighlightedParameter()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                board[i, j].IsNextToNextHighlighted = false;
            }
        }
    }

    public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove)
    {
        if(GameManager.Instance.GameMode == GameMode.Online)
        {
            int viewId = -1;
            if (pieceToMove != null)
            {
                viewId = pieceToMove.GetComponent<PhotonView>().ViewID;
            }
            thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, viewId);
        }
        else
        {
            board[pieceToMove.Row_ID, pieceToMove.Coloum_ID].SetBlockPiece(false, null);

            StartCoroutine(MovePiece(pieceToMove.transform, board[targetRow, targetCol].transform.position));
            AudioManager.Instance.PlayPieceMoveSound();
            board[targetRow, targetCol].SetBlockPiece(true, pieceToMove);
        }
        
    }

    [PunRPC]
    public void UpdateGrid(int targetRow, int targetCol, int viewId)
    {
        Piece piece = null;
        if (viewId != -1)
        {
            piece = PhotonView.Find(viewId).GetComponent<Piece>();
            board[piece.Row_ID, piece.Coloum_ID].SetBlockPiece(false, null);

            StartCoroutine(MovePiece(piece.transform, board[targetRow, targetCol].transform.position));
            AudioManager.Instance.PlayPieceMoveSound();
        }
        board[targetRow, targetCol].SetBlockPiece((viewId != -1), piece);
    }

    private IEnumerator MovePiece(Transform pieceToMove, Vector3 targetPos)
    {
        float time = 0.25f;
        float elapcedTime = 0;

        Vector3 initialPos = pieceToMove.position;

        while (elapcedTime <  time)
        {
            elapcedTime += Time.deltaTime;
            Vector3 pos = Vector3.Lerp(initialPos, targetPos, elapcedTime / time);
            pieceToMove.position = pos;
            yield return null;
        }
        pieceToMove.position = targetPos;
    }

    public void ResetGameplay()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                if(board[i,j].Piece != null)
                {
                    Destroy(board[i, j].Piece.gameObject);
                }
                Destroy(board[i, j].gameObject);
            }
        }

        highlightedBlocks.Clear();
        whitePieces.Clear();
        blackPieces.Clear();
        selectedPiece = null;
    }
}
