using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameplayController : Singleton<GameplayController>
{
    public Block[,] board = new Block[8, 8];
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

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

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < 8 && col >= 0 && col < 8;
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
        whitePieces.Clear();
        blackPieces.Clear();
    }
}
