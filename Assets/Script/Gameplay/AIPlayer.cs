using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class AIPlayer : Player
    {
        private WaitForSeconds waitForSeconds;

        public override void SetPlayer(int playerNumber, PieceType pieceType)
        {
            base.SetPlayer(playerNumber, pieceType);

            waitForSeconds = new WaitForSeconds(1f);
            GameplayController.Instance.DisablePieceInteractble(playerID);
        }

        public override void PlayTurn()
        {
            movablePieces.Clear();
            GameplayController.Instance.CheckMovablePieces(playerID, movablePieces);

            if (movablePieces.Count > 0)
            {
                StartCoroutine(PlayAITurn());
            }
        }

        private IEnumerator PlayAITurn()
        {
            SetMovablePosition();
            yield return waitForSeconds;
            CheckPieceMove();
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
                if (GameManager.Instance.GameMode == GameMode.Online)
                {
                    piece.PhotonView.RPC(nameof(piece.Destroy), RpcTarget.All);
                }
                else
                {
                    piece.Destroy();
                }
                hasDeleted = true;
                targetBlock.IsNextToNextHighlighted = false;
            }

            UpdateGrid(targetBlock.Row_ID, targetBlock.Coloum_ID, selectedPiece);

            yield return new WaitForSeconds(0.5f);


            if (!selectedPiece.IsCrownedKing && ((selectedPiece.Player_ID == 2 && selectedPiece.Row_ID == 7) ||
                (selectedPiece.Player_ID == 1 && selectedPiece.Row_ID == 0)))
            {
                selectedPiece.SetCrownKing();
                moveInfo.hasCrownSet = true;
            }

            //yield return new WaitForSeconds(0.25f);

            selectedPiece = targetBlock.Piece;

            if (hasDeleted && GameplayController.Instance.CanPieceKill(selectedPiece) /*CanMove()*/)
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
                GameManager.Instance.SwitchTurn();
                ResetNextToNextHighlightedBlock();
            }
        }

        public void UpdateGrid(int targetRow, int targetCol, Piece pieceToMove)
        {
            GameplayController.Instance.board[pieceToMove.Row_ID, pieceToMove.Coloum_ID].SetBlockPiece(false, null);

            StartCoroutine(MovePiece(pieceToMove, GameplayController.Instance.board[targetRow, targetCol]));
            AudioManager.Instance.PlayPieceMoveSound();
            GameplayController.Instance.board[targetRow, targetCol].SetBlockPiece(true, pieceToMove);

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

        private void CheckPieceMove()
        {
            movablePieces.Shuffle();

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                if (piece.safeDoubleKillerBlockPositions.Count > 0)
                {
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
                    selectedPiece = piece;
                    BoardPosition position = piece.movableBlockPositions[0];
                    Block block = GameplayController.Instance.board[position.row_ID, position.col_ID];

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }
        }
    }
}
