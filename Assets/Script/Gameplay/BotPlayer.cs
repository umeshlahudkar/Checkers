using System.Collections;
using UnityEngine;

namespace Gameplay
{
    public class BotPlayer : Player
    {
        private readonly WaitForSeconds waitForSeconds = new(1f);

        protected override void OnTurnReady()
        {
            StartCoroutine(PlayAITurn());
        }

        private IEnumerator PlayAITurn()
        {
            SetMovablePosition();
            yield return waitForSeconds;
            CheckPieceMove();
        }

        protected override void ContinueAfterKill(Piece selectedPiece)
        {
            selectedPiece.ResetAllList();
            ServiceLocator.Get<GameplayController>().SetAdjacentKillPosition(selectedPiece);
            BoardPosition position = default;

            if (selectedPiece.safeKillerBlockPositions.Count > 0)
            {
                position = selectedPiece.safeKillerBlockPositions[0];
            }
            else if (selectedPiece.killerBlockPositions.Count > 0)
            {
                position = selectedPiece.killerBlockPositions[0];
            }

            Block b = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
            b.IsNextToNextHighlighted = true;
            OnHighlightedTargetBlockClick(b);
            ResetNextToNextHighlightedBlock();
        }

        private void SetMovablePosition()
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                piece.ResetAllList();
                ServiceLocator.Get<GameplayController>().SetPiecePosition(piece);
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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

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
                    Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

                    OnHighlightedTargetBlockClick(block);
                    return;
                }
            }
        }
    }
}
