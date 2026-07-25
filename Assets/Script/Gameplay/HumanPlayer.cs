using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Gameplay
{
    public class HumanPlayer : Player
    {
        protected override void OnTurnReady()
        {
            HighlightMovablePieceBlock();
        }

        // Deliberately doesn't reuse OnHighlightedPieceClick - once already mid-chain with this
        // piece, only further captures may be offered (never a quiet move, which would let the
        // player illegally bail out of a still-mandatory continuation).
        protected override void ContinueAfterKill(Piece selectedPiece)
        {
            selectedPiece.ResetAllList();
            selectedPiece.captureSequences = ServiceLocator.Get<MoveGenerator>().GetLegalContinuations(selectedPiece);

            Block block = ServiceLocator.Get<GameplayController>().board[selectedPiece.Row_ID, selectedPiece.Coloum_ID];
            block.HighlightPieceBlock();
            highlightedBlocks.Add(block);

            HighlightCaptureSequences(selectedPiece.captureSequences);
        }

        private void HighlightMovablePieceBlock()
        {
            for(int i = 0; i < movablePieces.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[movablePieces[i].Row_ID, movablePieces[i].Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);
            }
        }

        // Suggests the same "objectively best" move BotPlayer's Hard difficulty would play,
        // regardless of this match's actual bot difficulty - runs the same minimax search (see
        // BotMinimax.HardDepth) on a background thread, same as BotPlayer's own turn. Only
        // meaningful at the start of a turn, before a piece is selected (see
        // GamePage.RefreshHintUndoButtons, which gates the button to that same window).
        public void ShowHint()
        {
            StartCoroutine(ShowHintRoutine());
        }

        private IEnumerator ShowHintRoutine()
        {
            // Prevent a second click from starting an overlapping search while this one's still
            // running - RefreshHintUndoButtons (called below) re-enables it once we're done.
            ServiceLocator.Get<GamePageManager>().GamePage.SetHintUndoInteractable(false);

            Task<AIMoveOption?> task = BotMinimax.StartSearch(Player_ID, BotMinimax.HardDepth);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            ServiceLocator.Get<GamePageManager>().GamePage.RefreshHintUndoButtons();

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                yield break;
            }

            BotMinimax.AIMove? bestMove = BotMinimax.ResolveMove(task.Result);
            if (!bestMove.HasValue) { yield break; }

            BotMinimax.AIMove move = bestMove.Value;
            BoardPosition landing = move.Sequence != null ? move.Sequence.Landings[0] : move.Position;

            ServiceLocator.Get<GameplayController>().ShowHintHighlight(move.Piece.Row_ID, move.Piece.Coloum_ID, landing.row_ID, landing.col_ID);
        }

        public override void OnHighlightedPieceClick(Piece clickedPiece)
        {
            ServiceLocator.Get<GameplayController>().ClearHintHighlight();
            ResetHighlightedBlocks();

            if (ServiceLocator.Get<MoveGenerator>().CanPieceMove(clickedPiece))
            {
                SelectPieceForNewMove(clickedPiece);

                Block block = ServiceLocator.Get<GameplayController>().board[clickedPiece.Row_ID, clickedPiece.Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);

                clickedPiece.ResetAllList();
                ServiceLocator.Get<MoveGenerator>().SetPiecePosition(clickedPiece);

                HighlightMovementBlocks(clickedPiece);
            }
            else
            {
                clickedPiece.PlayShakeAnimation();
                HighlightMovablePieceBlock();
            }
        }

        private void HighlightMovementBlocks(Piece clickedPiece)
        {
            HighlightCaptureSequences(clickedPiece.captureSequences);
            HighlightBlocks(clickedPiece.movablePositions, isKillMove: false);
        }

        private void HighlightCaptureSequences(List<CaptureSequence> sequences)
        {
            for (int i = 0; i < sequences.Count; i++)
            {
                CaptureSequence sequence = sequences[i];
                BoardPosition landing = sequence.Landings[0];

                Block block = ServiceLocator.Get<GameplayController>().board[landing.row_ID, landing.col_ID];
                block.HighlightNextMoveBlock(true);
                block.CapturedPosition = sequence.Captured[0];
                highlightedBlocks.Add(block);
                nextToNexthighlightedBlocks.Add(block);
            }
        }

        private void HighlightBlocks(List<BoardPosition> positions, bool isKillMove)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[positions[i].row_ID, positions[i].col_ID];
                block.HighlightNextMoveBlock(isKillMove);
                highlightedBlocks.Add(block);

                if (isKillMove)
                {
                    nextToNexthighlightedBlocks.Add(block);
                }
            }
        }
    }
}
