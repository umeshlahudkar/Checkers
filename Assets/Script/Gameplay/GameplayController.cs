using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GameplayController : Singleton<GameplayController>
{
    [SerializeField] private PhotonView thisPhotonView;

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

    #region AI
    //AI
    public bool CanMove(PieceType pieceType)
    {
        if (pieceType == PieceType.Black)
        {
            return CanBlackPieceMove();
        }
        else if (pieceType == PieceType.White)
        {
            return CanWhitePieceMove();
        }

        return false;
    }

    //AI
    private bool CanWhitePieceMove()
    {
        bool moveFound = false;
        foreach (Piece piece in whitePieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;
            Block pieceBlock = board[row, coloum];

            // diagonally down left
            moveFound |= CheckPieceCanMove(row + 1, coloum - 1, row + 2, coloum - 2, PieceType.White);
            //diagonally down right
            moveFound |= CheckPieceCanMove(row + 1, coloum + 1, row + 2, coloum + 2, PieceType.White);

            if (piece.IsCrownedKing)
            {
                // diagonally up left
                moveFound |= CheckPieceCanMove(row - 1, coloum - 1, row - 2, coloum - 2, PieceType.White);
                //diagonally up right
                moveFound |= CheckPieceCanMove(row - 1, coloum + 1, row - 2, coloum + 2, PieceType.White);
            }

            if (moveFound)
            {
                break;
            }
        }
        return moveFound;
    }

    //AI
    private bool CanBlackPieceMove()
    {
        bool moveFound = false;
        foreach (Piece piece in blackPieces)
        {
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            Block pieceBlock = board[row, coloum];

            // diagonally up left
            moveFound |= CheckPieceCanMove(row - 1, coloum - 1, row - 2, coloum - 2, PieceType.Black);
            // diagonally up right
            moveFound |= CheckPieceCanMove(row - 1, coloum + 1, row - 2, coloum + 2, PieceType.Black);

            if (piece.IsCrownedKing)
            {
                // diagonally down left
                moveFound |= CheckPieceCanMove(row + 1, coloum - 1, row + 2, coloum - 2, PieceType.Black);
                //diagonally down right
                moveFound |= CheckPieceCanMove(row + 1, coloum + 1, row + 2, coloum + 2, PieceType.Black);
            }

            if (moveFound)
            {
                break;
            }
        }
        return moveFound;
    }

    //AI
    private bool CheckPieceCanMove(int row, int col, int jumpRow, int jumpCol, PieceType pieceType)
    {
        if (!IsValidPosition(row, col)) { return false; }

        bool moveFound = false;

        if ((!board[row, col].IsPiecePresent) ||
            (board[row, col].IsPiecePresent && pieceType != board[row, col].Piece.PieceType &&
                IsValidPosition(jumpRow, jumpCol) && !board[jumpRow, jumpCol].IsPiecePresent))
        {
            moveFound = true;
        }
        return moveFound;
    }

    //AI 
    public void CheckMovablePieces(PieceType pieceType, List<Piece> movablePieces)
    {
        if (pieceType == PieceType.Black)
        {
            CheckBlackMovablePieces(movablePieces);
        }
        else if (pieceType == PieceType.White)
        {
            CheckWhiteMovablePieces(movablePieces);
        }
    }

    //AI 
    private void CheckWhiteMovablePieces(List<Piece> movablePieces)
    {
        foreach (Piece piece in whitePieces)
        {
            bool moveFound = false;
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            // diagonally down left
            moveFound |= CheckPieceCanMove(row + 1, coloum - 1, row + 2, coloum - 2, PieceType.White);
            //diagonally down right
            moveFound |= CheckPieceCanMove(row + 1, coloum + 1, row + 2, coloum + 2, PieceType.White);

            if (piece.IsCrownedKing)
            {
                // diagonally up left
                moveFound |= CheckPieceCanMove(row - 1, coloum - 1, row - 2, coloum - 2, PieceType.White);
                //diagonally up right
                moveFound |= CheckPieceCanMove(row - 1, coloum + 1, row - 2, coloum + 2, PieceType.White);
            }

            if (moveFound)
            {
                movablePieces.Add(piece);
            }
        }
    }

    //AI 
    private void CheckBlackMovablePieces(List<Piece> movablePieces)
    {
        foreach (Piece piece in blackPieces)
        {
            bool moveFound = false;
            int row = piece.Row_ID;
            int coloum = piece.Coloum_ID;

            // diagonally up left
            moveFound |= CheckPieceCanMove(row - 1, coloum - 1, row - 2, coloum - 2, PieceType.Black);
            // diagonally up right
            moveFound |= CheckPieceCanMove(row - 1, coloum + 1, row - 2, coloum + 2, PieceType.Black);

            if (piece.IsCrownedKing)
            {
                // diagonally down left
                moveFound |= CheckPieceCanMove(row + 1, coloum - 1, row + 2, coloum - 2, PieceType.Black);
                //diagonally down right
                moveFound |= CheckPieceCanMove(row + 1, coloum + 1, row + 2, coloum + 2, PieceType.Black);
            }

            if (moveFound)
            {
                movablePieces.Add(piece);
            }
        }
    }

    //AI 
    public void SetMovablePosition(Piece piece)
    {
        if (piece.PieceType == PieceType.Black)
        {
            SetBlackPieceMovablePositions(piece);
        }
        else if (piece.PieceType == PieceType.White)
        {
            SetWhitePieceMovablePositions(piece);
        }
    }

    //AI 
    private void SetWhitePieceMovablePositions(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;
        PieceType pieceType = piece.PieceType;

        bool canMove;

        canMove = CanMoveAtAdjacentBlock(row + 1, coloum - 1);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row + 1, coloum - 1));
        }

        canMove = CanMoveAtJumpBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceType);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row + 2, coloum - 2));
        }

        canMove = CanMoveAtAdjacentBlock(row + 1, coloum + 1);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row + 1, coloum + 1));
        }

        canMove = CanMoveAtJumpBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceType);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row + 2, coloum + 2));
        }

        if (piece.IsCrownedKing)
        {
            canMove = CanMoveAtAdjacentBlock(row - 1, coloum - 1);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row - 1, coloum - 1));
            }

            canMove = CanMoveAtJumpBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceType);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row - 2, coloum - 2));
            }

            canMove = CanMoveAtAdjacentBlock(row - 1, coloum + 1);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row - 1, coloum + 1));
            }

            canMove = CanMoveAtJumpBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceType);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row - 2, coloum + 2));
            }
        }
    }

    //AI 
    private void SetBlackPieceMovablePositions(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;
        PieceType pieceType = piece.PieceType;

        bool canMove;

        canMove = CanMoveAtAdjacentBlock(row - 1, coloum - 1);
        if(canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row - 1, coloum - 1));
        }

        canMove = CanMoveAtJumpBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceType);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row - 2, coloum - 2));
        }

        canMove = CanMoveAtAdjacentBlock(row - 1, coloum + 1);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row - 1, coloum + 1));
        }

        canMove = CanMoveAtJumpBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceType);
        if (canMove)
        {
            piece.movableBlockPositions.Add(new BoardPosition(row - 2, coloum + 2));
        }

        if(piece.IsCrownedKing)
        {
            canMove = CanMoveAtAdjacentBlock(row + 1, coloum - 1);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row + 1, coloum - 1));
            }

            canMove = CanMoveAtJumpBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceType);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row + 2, coloum - 2));
            }

            canMove = CanMoveAtAdjacentBlock(row + 1, coloum + 1);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row + 1, coloum + 1));
            }

            canMove = CanMoveAtJumpBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceType);
            if (canMove)
            {
                piece.movableBlockPositions.Add(new BoardPosition(row + 2, coloum + 2));
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

    //AI 
    private bool CanMoveAtJumpBlock(int row, int col, int jumpRow, int jumpCol, PieceType pieceType)
    {
        if (!IsValidPosition(jumpRow, jumpCol)) { return false; }

        if(!board[jumpRow, jumpCol].IsPiecePresent && IsValidPosition(row, col) &&
            board[row, col].IsPiecePresent && pieceType != board[row, col].Piece.PieceType)
        {
            return true;
        }
        
        return false;
    }

    //AI 
    public bool CanKillOpponentPiece(Piece piece)
    {
        if (piece.PieceType == PieceType.Black)
        {
            return CanKillOpponent_BlackPiece(piece);
        }
        else if (piece.PieceType == PieceType.White)
        {
            return CanKillOpponent_WhitePiece(piece);
        }
        return false;
    }

    //AI 
    private bool CanKillOpponent_WhitePiece(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;
        PieceType pieceType = piece.PieceType;

        bool canKill = false;

        canKill |= CanMoveAtJumpBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceType);
        canKill |= CanMoveAtJumpBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceType);

        if (piece.IsCrownedKing)
        {
            canKill |= CanMoveAtJumpBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceType);
            canKill |= CanMoveAtJumpBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceType);
        }

        return canKill;
    }

    //AI 
    private bool CanKillOpponent_BlackPiece(Piece piece)
    {
        int row = piece.Row_ID;
        int coloum = piece.Coloum_ID;
        PieceType pieceType = piece.PieceType;

        bool canKill = false;

        canKill |= CanMoveAtJumpBlock(row - 1, coloum - 1, row - 2, coloum - 2, pieceType);
        canKill |= CanMoveAtJumpBlock(row - 1, coloum + 1, row - 2, coloum + 2, pieceType);

        if (piece.IsCrownedKing)
        {
            canKill |= CanMoveAtJumpBlock(row + 1, coloum - 1, row + 2, coloum - 2, pieceType);
            canKill |= CanMoveAtJumpBlock(row + 1, coloum + 1, row + 2, coloum + 2, pieceType);
        }

        return canKill;
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
        }

        //UpdateGrid(selectedPiece.Row_ID, selectedPiece.Coloum_ID, null);
        UpdateGrid(block.Row_ID, block.Coloum_ID, selectedPiece);

        //selectedPiece.transform.position = block.transform.position;
        //yield return StartCoroutine(MovePiece(selectedPiece.transform, block.transform.position));

        yield return new WaitForSeconds(0.25f);

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

        if (hasDeleted && CanMove())
        {
            CheckNextMove(selectedPiece);
        }
        else
        {
           GameManager.Instance.SwitchTurn();
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
