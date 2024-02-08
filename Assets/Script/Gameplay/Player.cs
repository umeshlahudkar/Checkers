using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class Player : MonoBehaviour
    {
        private int playerID;
        private PieceType pieceType;

        [SerializeField] private bool isAI;

        private readonly List<Block> highlightedBlocks = new();
        private readonly List<Block> nextToNexthighlightedBlocks = new();
        private readonly List<Piece> movablePieces = new();

        private Piece selectedPiece;
        private PhotonView thisPhotonView;

        public PieceType PieceType { get { return pieceType; } }

        public void SetPlayer(int playerNumber, PieceType pieceType, bool isAI)
        {
            this.playerID = playerNumber;
            this.pieceType = pieceType;
            this.isAI = isAI;
        }

        public bool CanPlay()
        {
            if(GameplayController.Instance.CanMove(playerID))
            {
                Invoke(nameof(PlayTurn), 0.5f);
                return true;
            }
            return false;
        }

        private void PlayTurn()
        {
            movablePieces.Clear();
            GameplayController.Instance.CheckMovablePieces(playerID, movablePieces);

            if (movablePieces.Count > 0)
            {
                if(playerID == 2 && isAI)
                {
                    SetMovablePosition();
                    CheckPieceMove();
                }
                else
                {
                    HighlightMovablePieceBlock();
                }
            }
        }

        private void HighlightMovablePieceBlock()
        {
            for(int i = 0; i < movablePieces.Count; i++)
            {
                Block block = GameplayController.Instance.board[movablePieces[i].Row_ID, movablePieces[i].Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);
            }
        }

        private void ResetHighlightedBlocks()
        {
            for (int i = 0; i < highlightedBlocks.Count; i++)
            {
                highlightedBlocks[i].ResetBlock();
            }
            highlightedBlocks.Clear();
        }

        private void ResetNextToNextHighlightedBlock()
        {
            for (int i = 0; i < nextToNexthighlightedBlocks.Count; i++)
            {
                nextToNexthighlightedBlocks[i].IsNextToNextHighlighted = false;
            }
            nextToNexthighlightedBlocks.Clear();
        }

        public void OnHighlightedPieceClick(Piece clickedPiece)
        {
            ResetHighlightedBlocks();

            if (GameplayController.Instance.CanPieceMove(clickedPiece))
            {
                selectedPiece = clickedPiece;

                Block block = GameplayController.Instance.board[clickedPiece.Row_ID, clickedPiece.Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);

                clickedPiece.ResetAllList();
                GameplayController.Instance.SetPiecePosition(clickedPiece);

                HighlightMovementBlocks(clickedPiece);
            }
            else
            {
                HighlightMovablePieceBlock();
            }
        }

        public void OnHighlightedTargetBlockClick(Block block)
        {
            //GameManager.Instance.ResetTurnMissCount();
            ResetHighlightedBlocks();

            bool hasDeleted = false;

            if (block.IsNextToNextHighlighted)
            {
                int row = block.Row_ID;
                int coloum = block.Coloum_ID;

                int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
                int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

                Piece piece = GameplayController.Instance.board[targetRow, targetCol].Piece;
                if (GameManager.Instance.GameMode == GameMode.Online)
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

            //yield return new WaitForSeconds(0.5f);  //0.25

            GameplayUIController.Instance.StopPlayerHighlightAnim();

            if (!selectedPiece.IsCrownedKing && ((selectedPiece.Player_ID == 2 && selectedPiece.Row_ID == 7) ||
                (selectedPiece.Player_ID == 1 && selectedPiece.Row_ID == 0)))
            {
                if (GameManager.Instance.GameMode == GameMode.Online)
                {
                    selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
                }
                else
                {
                    selectedPiece.SetCrownKing();
                }
            }

            selectedPiece = block.Piece;

            if (hasDeleted && GameplayController.Instance.CanPieceKill(selectedPiece) /*CanMove()*/)
            {
                if (GameManager.Instance.GameMode == GameMode.PVC && isAI && GameManager.Instance.CurrentTurn == playerID)
                {
                    selectedPiece.ResetAllList();
                    GameplayController.Instance.SetAdjacentKillPosition(selectedPiece);
                    BoardPosition position = default;

                    if (selectedPiece.safeKillerBlockPositions.Count > 0)
                    {
                        position = selectedPiece.safeKillerBlockPositions[0];
                    }
                    else if (selectedPiece.killerBlockPositions.Count > 0)
                    {
                        position = selectedPiece.killerBlockPositions[0];
                    }

                    Block b = GameplayController.Instance.board[position.row_ID, position.col_ID];
                    b.IsNextToNextHighlighted = true;
                    OnHighlightedTargetBlockClick(b);
                    ResetNextToNextHighlightedBlock();
                }
                else
                {
                    //CheckNextMove(selectedPiece);
                    OnHighlightedPieceClick(selectedPiece);
                }
            }
            else
            {
                GameManager.Instance.SwitchTurn();
                ResetNextToNextHighlightedBlock();
                //ResetBlocksNextToNextHighlightedParameter();
            }
        }

        public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove)
        {
            if (GameManager.Instance.GameMode == GameMode.Online)
            {
                int viewId = -1;
                if (pieceToMove != null)
                {
                    viewId = pieceToMove.PhotonView.ViewID;
                }
                thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, viewId);
            }
            else
            {
                GameplayController.Instance.board[pieceToMove.Row_ID, pieceToMove.Coloum_ID].SetBlockPiece(false, null);

                StartCoroutine(MovePiece(pieceToMove.transform, GameplayController.Instance.board[targetRow, targetCol].transform.position));
                AudioManager.Instance.PlayPieceMoveSound();
                GameplayController.Instance.board[targetRow, targetCol].SetBlockPiece(true, pieceToMove);
            }

        }

        [PunRPC]
        public void UpdateGrid(int targetRow, int targetCol, int viewId)
        {
            Piece piece = null;
            if (viewId != -1)
            {
                piece = PhotonView.Find(viewId).GetComponent<Piece>();
                GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID].SetBlockPiece(false, null);

                StartCoroutine(MovePiece(piece.transform, GameplayController.Instance.board[targetRow, targetCol].transform.position));
                AudioManager.Instance.PlayPieceMoveSound();
            }
            GameplayController.Instance.board[targetRow, targetCol].SetBlockPiece((viewId != -1), piece);
        }

        private IEnumerator MovePiece(Transform pieceToMove, Vector3 targetPos)
        {
            float time = 0.25f;
            float elapcedTime = 0;

            Vector3 initialPos = pieceToMove.position;

            while (elapcedTime < time)
            {
                elapcedTime += Time.deltaTime;
                Vector3 pos = Vector3.Lerp(initialPos, targetPos, elapcedTime / time);
                pieceToMove.position = pos;
                yield return null;
            }
            pieceToMove.position = targetPos;
        }

        private void HighlightMovementBlocks(Piece clickedPiece)
        {
            List<BoardPosition> safeKillerPosition = clickedPiece.safeKillerBlockPositions;
            for (int i = 0; i < safeKillerPosition.Count; i++)
            {
                Block block = GameplayController.Instance.board[safeKillerPosition[i].row_ID, safeKillerPosition[i].col_ID];
                block.HighlightNextMoveBlock(true);
                highlightedBlocks.Add(block);
                nextToNexthighlightedBlocks.Add(block);
            }

            List<BoardPosition> killerPosition = clickedPiece.killerBlockPositions;
            for (int i = 0; i < killerPosition.Count; i++)
            {
                Block block = GameplayController.Instance.board[killerPosition[i].row_ID, killerPosition[i].col_ID];
                block.HighlightNextMoveBlock(true);
                highlightedBlocks.Add(block);
                nextToNexthighlightedBlocks.Add(block);
            }

            List<BoardPosition> safeMovementPosition = clickedPiece.safeMovableBlockPositions;
            for (int i = 0; i < safeMovementPosition.Count; i++)
            {
                Block block = GameplayController.Instance.board[safeMovementPosition[i].row_ID, safeMovementPosition[i].col_ID];
                block.HighlightNextMoveBlock();
                highlightedBlocks.Add(block);
            }

            List<BoardPosition> movementPosition = clickedPiece.movableBlockPositions;
            for (int i = 0; i < movementPosition.Count; i++)
            {
                Block block = GameplayController.Instance.board[movementPosition[i].row_ID, movementPosition[i].col_ID];
                block.HighlightNextMoveBlock();
                highlightedBlocks.Add(block);
            }
        }

        // AI


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
                    //BoardPosition position = piece.safeDoubleKillerBlockPositions[0];
                    //GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                    //MovePiece(piece, position);

                    selectedPiece = piece;
                    BoardPosition position = piece.safeDoubleKillerBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                    block.IsNextToNextHighlighted = true;
                    nextToNexthighlightedBlocks.Add(block);

                    OnHighlightedTargetBlockClick(block);

                    return;
                }
            }

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.doubleKillerBlockPositions.Count > 0)
                {
                    //BoardPosition position = piece.doubleKillerBlockPositions[0];
                    //GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                    //MovePiece(piece, position);

                    selectedPiece = piece;
                    BoardPosition position = piece.doubleKillerBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                    block.IsNextToNextHighlighted = true;
                    nextToNexthighlightedBlocks.Add(block);

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.safeKillerBlockPositions.Count > 0)
                {
                    //BoardPosition position = piece.safeKillerBlockPositions[0];
                    //GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                    //MovePiece(piece, position);

                    selectedPiece = piece;
                    BoardPosition position = piece.safeKillerBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                    block.IsNextToNextHighlighted = true;
                    nextToNexthighlightedBlocks.Add(block);

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.killerBlockPositions.Count > 0)
                {
                    //BoardPosition position = piece.killerBlockPositions[0];
                    //GameplayController.Instance.board[position.row_ID, position.col_ID].IsNextToNextHighlighted = true;
                    //MovePiece(piece, position);

                    selectedPiece = piece;
                    BoardPosition position = piece.killerBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                    block.IsNextToNextHighlighted = true;
                    nextToNexthighlightedBlocks.Add(block);

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }


            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.safeMovableBlockPositions.Count > 0)
                {
                    //MovePiece(piece, piece.safeMovableBlockPositions[0]);

                    selectedPiece = piece;
                    BoardPosition position = piece.safeMovableBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }


            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.movableBlockPositions.Count > 0)
                {
                    //MovePiece(piece, piece.movableBlockPositions[0]);

                    selectedPiece = piece;
                    BoardPosition position = piece.movableBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];

                    OnHighlightedTargetBlockClick(block);
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

