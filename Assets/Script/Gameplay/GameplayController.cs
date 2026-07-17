using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameplayController : Service<GameplayController>
{
    // The four diagonal directions a piece can move in: down-left, down-right, up-left, up-right.
    private static readonly (int dRow, int dCol)[] DiagonalDirections =
    {
        (1, -1), (1, 1), (-1, -1), (-1, 1)
    };

    public Block[,] board = new Block[8, 8];
    public List<Piece> whitePieces = new();
    public List<Piece> blackPieces = new();

    // Player 2 (white) advances down the board, player 1 (black) advances up it.
    private static int ForwardDirection(int playerID)
    {
        return playerID == 2 ? 1 : -1;
    }

    // Non-king pieces may only move/kill in their forward direction; kings move in all four.
    private IEnumerable<(int dRow, int dCol)> GetMoveDirections(Piece piece)
    {
        int forward = ForwardDirection(piece.Player_ID);
        foreach ((int dRow, int dCol) dir in DiagonalDirections)
        {
            if (piece.IsCrownedKing || dir.dRow == forward)
            {
                yield return dir;
            }
        }
    }

    private List<Piece> GetPiecesForPlayer(int playerID)
    {
        return playerID == 1 ? blackPieces : whitePieces;
    }

    public bool CanMove(int playerNumber)
    {
        List<Piece> pieces = GetPiecesForPlayer(playerNumber);
        for (int i = 0; i < pieces.Count; i++)
        {
            if (CanPieceMove(pieces[i]))
            {
                return true;
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
        int col = piece.Coloum_ID;
        int playerID = piece.Player_ID;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            if (CheckPieceCanMove(playerID, row + dir.dRow, col + dir.dCol, row + dir.dRow * 2, col + dir.dCol * 2))
            {
                return true;
            }
        }
        return false;
    }

    public bool CanPieceKill(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            if (CanKillAdjecentPiece(piece, row + dir.dRow * 2, col + dir.dCol * 2))
            {
                return true;
            }
        }
        return false;
    }

    //AI
    public void CheckMovablePieces(int playerID, List<Piece> movablePieces)
    {
        List<Piece> pieces = GetPiecesForPlayer(playerID);
        for (int i = 0; i < pieces.Count; i++)
        {
            if (CanPieceMove(pieces[i]))
            {
                movablePieces.Add(pieces[i]);
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

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            CheckDiagonalAdjacentKill(piece, row + dir.dRow * 2, col + dir.dCol * 2);
        }
    }

    private void SetAdjacentMovePosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            CheckDiagonalAdjacentMove(piece, row + dir.dRow, col + dir.dCol);
        }
    }

    private void SetDoubleKillPosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            CheckForDoubleKill(piece, row + dir.dRow * 2, col + dir.dCol * 2);
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

    // A piece is unsafe if an enemy piece adjacent to it could jump over it to the opposite square.
    // The enemy can do so unconditionally from its own forward direction, or from its backward
    // direction only if it's a king.
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

        int forward = ForwardDirection(playerID);

        foreach ((int dRow, int dCol) dir in DiagonalDirections)
        {
            int enemyRow = row + dir.dRow;
            int enemyCol = col + dir.dCol;

            if (!IsValidPosition(enemyRow, enemyCol) || !board[enemyRow, enemyCol].IsPiecePresent)
            {
                continue;
            }

            Piece enemy = board[enemyRow, enemyCol].Piece;
            if (enemy.Player_ID == playerID)
            {
                continue;
            }

            bool enemyMovingBackward = dir.dRow != forward;
            if (enemyMovingBackward && !enemy.IsCrownedKing)
            {
                continue;
            }

            int landingRow = row - dir.dRow;
            int landingCol = col - dir.dCol;

            if (IsValidPosition(landingRow, landingCol) && !board[landingRow, landingCol].IsPiecePresent)
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
