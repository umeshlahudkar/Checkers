using System;
using System.Collections;
using System.Collections.Generic;
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

            List<BoardPosition> positions = selectedPiece.safeKillerBlockPositions.Count > 0
                ? selectedPiece.safeKillerBlockPositions
                : selectedPiece.killerBlockPositions;

            BoardPosition position = positions[0];

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

            // Priority order: a safe double-kill beats an unsafe one, which beats a safe single
            // kill, and so on down to a plain (unsafe) move. The first list with any positions wins.
            if (TryMakeMove(p => p.safeDoubleKillerBlockPositions, isKillMove: true)) { return; }
            if (TryMakeMove(p => p.doubleKillerBlockPositions, isKillMove: true)) { return; }
            if (TryMakeMove(p => p.safeKillerBlockPositions, isKillMove: true)) { return; }
            if (TryMakeMove(p => p.killerBlockPositions, isKillMove: true)) { return; }
            if (TryMakeMove(p => p.safeMovableBlockPositions, isKillMove: false)) { return; }
            TryMakeMove(p => p.movableBlockPositions, isKillMove: false);
        }

        private bool TryMakeMove(Func<Piece, List<BoardPosition>> getPositions, bool isKillMove)
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                List<BoardPosition> positions = getPositions(piece);
                if (positions.Count == 0) { continue; }

                selectedPiece = piece;
                BoardPosition position = positions[0];
                Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

                if (isKillMove)
                {
                    block.IsNextToNextHighlighted = true;
                    nextToNexthighlightedBlocks.Add(block);
                }

                OnHighlightedTargetBlockClick(block);
                return true;
            }
            return false;
        }
    }
}
