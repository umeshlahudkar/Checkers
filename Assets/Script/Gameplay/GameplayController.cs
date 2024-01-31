using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameplayController : Singleton<GameplayController>
{
    public Block[,] board = new Block[8, 8];
    public List<Piece> whitePieces = new List<Piece>();
    public List<Piece> blackPieces = new List<Piece>();

    [SerializeField] private List<Block> highlightedBlocks = new List<Block>();

    public Piece selectedPiece;


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
        ResetHighlightedBlocks();

        bool hasDeleted = false;

        if(block.IsNextToNextHighlighted)
        {
            int row = block.Row_ID;
            int coloum = block.Coloum_ID;

            int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
            int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

            Piece piece = board[targetRow, targetCol].Piece;
            piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
            hasDeleted = true;
        }
       
        GameManager.Instance.UpdateGrid(selectedPiece.Row_ID, selectedPiece.Coloum_ID, null);

      
        GameManager.Instance.UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);
      
        //selectedPiece.transform.position = block.transform.position;
        yield return StartCoroutine(MovePiece(selectedPiece.transform, block.transform.position));

        UIController.Instance.StopPlayerHighlightAnim();

        if ((selectedPiece.PieceType == PieceType.White && selectedPiece.Row_ID == 7) ||
            (selectedPiece.PieceType == PieceType.Black && selectedPiece.Row_ID == 0))
        {
            selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
        }

        selectedPiece = block.Piece;

        if (hasDeleted && CanMove())
        {
            CheckNextMove(selectedPiece);
        }
        else
        {
           GameManager.Instance.SwitchTurn();
        }
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

    public void ResetGameplay()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                Destroy(board[i, j].gameObject);
            }
        }

        highlightedBlocks.Clear();
        whitePieces.Clear();
        blackPieces.Clear();
        selectedPiece = null;
    }
}
