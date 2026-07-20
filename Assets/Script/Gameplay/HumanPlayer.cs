using System.Collections.Generic;

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

        public override void OnHighlightedPieceClick(Piece clickedPiece)
        {
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
