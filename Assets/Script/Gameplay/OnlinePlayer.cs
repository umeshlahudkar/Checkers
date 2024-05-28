using Gameplay;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class OnlinePlayer : Player
    {
        private PhotonView thisPhotonView;

        public PhotonView PhotonView { get { return thisPhotonView; } }

        private void Start()
        {
            thisPhotonView = GetComponent<PhotonView>();
            playerID = thisPhotonView.OwnerActorNr;
            pieceType = (playerID == 1) ? PieceType.Black : PieceType.White;
            GameManager.Instance.ListPlayer(this);
            turnMissCount = 0;

            Debug.Log("Online player instantiate");
        }

        public override void PlayTurn()
        {
            movablePieces.Clear();
            GameplayController.Instance.CheckMovablePieces(playerID, movablePieces);

            if (movablePieces.Count > 0)
            {
                HighlightMovablePieceBlock();
            }
        }
      
        public override void ShowMoveHint()
        {
            if (movablePieces.Count > 0)
            {
                ResetPlayer();
                SetMovablePosition();
                movablePieces.Shuffle();

                //for (int i = 0; i < movablePieces.Count; i++)
                //{
                //    Piece piece = movablePieces[i];
                //    if (piece.safeDoubleKillerBlockPositions.Count > 0)
                //    {
                //        selectedPiece = piece;
                //        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                //        pieceBlock.HighlightPieceBlock();

                //        Debug.Log("Safe double killer");

                //        //BoardPosition position = piece.safeDoubleKillerBlockPositions[0];
                //        //Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                //        //block.HighlightPieceBlock();
                //        //block.IsNextToNextHighlighted = true;
                //        //nextToNexthighlightedBlocks.Add(block);

                //        return;
                //    }
                //}

                //for (int i = 0; i < movablePieces.Count; i++)
                //{
                //    Piece piece = movablePieces[i];
                //    if (piece.doubleKillerBlockPositions.Count > 0)
                //    {
                //        selectedPiece = piece;
                //        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                //        pieceBlock.HighlightPieceBlock();

                //        Debug.Log("Unsafe double killer");


                //        //BoardPosition position = piece.doubleKillerBlockPositions[0];
                //        //Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                //        //block.IsNextToNextHighlighted = true;
                //        //nextToNexthighlightedBlocks.Add(block);

                //        //block.HighlightPieceBlock();
                //        return;
                //    }
                //}

                for (int i = 0; i < movablePieces.Count; i++)
                {
                    Piece piece = movablePieces[i];
                    if (piece.safeKillerBlockPositions.Count > 0)
                    {
                        Debug.Log("Safe killer");

                        selectedPiece = piece;
                        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                        pieceBlock.HighlightPieceBlock();
                        highlightedBlocks.Add(pieceBlock);

                        BoardPosition position = piece.safeKillerBlockPositions[0];
                        Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                        block.IsNextToNextHighlighted = true;
                        nextToNexthighlightedBlocks.Add(block);
                        highlightedBlocks.Add(block);

                        block.HighlightNextMoveBlock(true);
                        return;
                    }
                }

                for (int i = 0; i < movablePieces.Count; i++)
                {
                    Piece piece = movablePieces[i];
                    if (piece.safeMovableBlockPositions.Count > 0)
                    {
                        Debug.Log("Safe movable");

                        selectedPiece = piece;
                        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                        pieceBlock.HighlightPieceBlock();
                        highlightedBlocks.Add(pieceBlock);

                        BoardPosition position = piece.safeMovableBlockPositions[0];
                        Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                        block.HighlightNextMoveBlock();
                        highlightedBlocks.Add(block);
                        return;
                    }
                }

                for (int i = 0; i < movablePieces.Count; i++)
                {
                    Piece piece = movablePieces[i];
                    if (piece.killerBlockPositions.Count > 0)
                    {
                        Debug.Log("Unsafe killer");

                        selectedPiece = piece;
                        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                        pieceBlock.HighlightPieceBlock();
                        highlightedBlocks.Add(pieceBlock);

                        BoardPosition position = piece.killerBlockPositions[0];
                        Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                        block.IsNextToNextHighlighted = true;
                        nextToNexthighlightedBlocks.Add(block);
                        highlightedBlocks.Add(block);

                        block.HighlightNextMoveBlock(true);
                        return;
                    }
                }

                for (int i = 0; i < movablePieces.Count; i++)
                {
                    Piece piece = movablePieces[i];
                    if (piece.movableBlockPositions.Count > 0)
                    {
                        Debug.Log("Unsafe movable");

                        selectedPiece = piece;
                        Block pieceBlock = GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID];
                        pieceBlock.HighlightPieceBlock();
                        highlightedBlocks.Add(pieceBlock);

                        BoardPosition position = piece.movableBlockPositions[0];
                        Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];
                        block.HighlightNextMoveBlock();
                        highlightedBlocks.Add(block);

                        return;
                    }
                }
            }
        }

        private void HighlightMovablePieceBlock()
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Block block = GameplayController.Instance.board[movablePieces[i].Row_ID, movablePieces[i].Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);
            }
        }

        public override void OnHighlightedPieceClick(Piece clickedPiece)
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

        public override void OnHighlightedTargetBlockClick(Block targetBlock)
        {
            StartCoroutine(HandlePieceMovementAndPieceDelete(targetBlock));
        }

        private IEnumerator HandlePieceMovementAndPieceDelete(Block targetBlock)
        {
            turnMissCount = 0;
            ResetHighlightedBlocks();

            bool hasDeleted = false;

            //
            moveInfo = new MoveInfo();
            //moveInfo.piece = selectedPiece;
            moveInfo.currentBlock = targetBlock;
            moveInfo.previousBlock = GameplayController.Instance.board[selectedPiece.Row_ID, selectedPiece.Coloum_ID];

            GameplayController.Instance.AddLastMoveInfo(moveInfo);
            //

            if (targetBlock.IsNextToNextHighlighted)
            {
                int row = targetBlock.Row_ID;
                int coloum = targetBlock.Coloum_ID;

                int targetRow = row + (row > selectedPiece.Row_ID ? -1 : 1);
                int targetCol = coloum + (coloum > selectedPiece.Coloum_ID ? -1 : 1);

                Piece piece = GameplayController.Instance.board[targetRow, targetCol].Piece;

                //
                moveInfo.deletedPieceBlock = GameplayController.Instance.board[targetRow, targetCol];
                moveInfo.deletedPieceType = piece.PieceType;
                moveInfo.deletedPiecePlayerId = piece.Player_ID;
                moveInfo.wasDeletedPieceKrown = piece.IsCrownedKing;
                //
                piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
               
                hasDeleted = true;
                targetBlock.IsNextToNextHighlighted = false;
            }

            UpdateGrid(targetBlock.Row_ID, targetBlock.Coloum_ID, selectedPiece);

            yield return new WaitForSeconds(0.5f);


            if (!selectedPiece.IsCrownedKing && ((selectedPiece.Player_ID == 2 && selectedPiece.Row_ID == 7) ||
                (selectedPiece.Player_ID == 1 && selectedPiece.Row_ID == 0)))
            {
                selectedPiece.PhotonView.RPC(nameof(selectedPiece.SetCrownKing), RpcTarget.All);
                moveInfo.hasCrownSet = true;
            }

            //yield return new WaitForSeconds(0.25f);

            selectedPiece = targetBlock.Piece;

            if (hasDeleted && GameplayController.Instance.CanPieceKill(selectedPiece) /*CanMove()*/)
            {
               
                OnHighlightedPieceClick(selectedPiece);
            }
            else
            {
                GameManager.Instance.SwitchTurn();
                ResetNextToNextHighlightedBlock();
            }
        }

        public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove)
        {
            int viewId = -1;
            if (pieceToMove != null)
            {
                viewId = pieceToMove.PhotonView.ViewID;
            }
            thisPhotonView.RPC(nameof(UpdateGrid), RpcTarget.All, targetRow, targetCol, viewId);
        }

        [PunRPC]
        public void UpdateGrid(int targetRow, int targetCol, int viewId)
        {
            Piece piece = null;
            if (viewId != -1)
            {
                piece = PhotonView.Find(viewId).GetComponent<Piece>();
                GameplayController.Instance.board[piece.Row_ID, piece.Coloum_ID].SetBlockPiece(false, null);

                StartCoroutine(MovePiece(piece, GameplayController.Instance.board[targetRow, targetCol]));
                AudioManager.Instance.PlayPieceMoveSound();
            }
            GameplayController.Instance.board[targetRow, targetCol].SetBlockPiece((viewId != -1), piece);
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

        private void SetMovablePosition()
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                piece.ResetAllList();
                GameplayController.Instance.SetPiecePosition(piece);
            }
        }
    }
}
