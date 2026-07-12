using System.Collections.Generic;

namespace Gameplay
{
    public class HumanPlayer : Player
    {
        protected override void OnTurnReady()
        {
            HighlightMovablePieceBlock();
        }

        protected override void ContinueAfterKill(Piece selectedPiece)
        {
            OnHighlightedPieceClick(selectedPiece);
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

            if (ServiceLocator.Get<GameplayController>().CanPieceMove(clickedPiece))
            {
                selectedPiece = clickedPiece;

                Block block = ServiceLocator.Get<GameplayController>().board[clickedPiece.Row_ID, clickedPiece.Coloum_ID];
                block.HighlightPieceBlock();
                highlightedBlocks.Add(block);

                clickedPiece.ResetAllList();
                ServiceLocator.Get<GameplayController>().SetPiecePosition(clickedPiece);

                HighlightMovementBlocks(clickedPiece);
            }
            else
            {
                HighlightMovablePieceBlock();
            }
        }

        private void HighlightMovementBlocks(Piece clickedPiece)
        {
            List<BoardPosition> safeKillerPosition = clickedPiece.safeKillerBlockPositions;
            for (int i = 0; i < safeKillerPosition.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[safeKillerPosition[i].row_ID, safeKillerPosition[i].col_ID];
                block.HighlightNextMoveBlock(true);
                highlightedBlocks.Add(block);
                nextToNexthighlightedBlocks.Add(block);
            }

            List<BoardPosition> killerPosition = clickedPiece.killerBlockPositions;
            for (int i = 0; i < killerPosition.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[killerPosition[i].row_ID, killerPosition[i].col_ID];
                block.HighlightNextMoveBlock(true);
                highlightedBlocks.Add(block);
                nextToNexthighlightedBlocks.Add(block);
            }

            List<BoardPosition> safeMovementPosition = clickedPiece.safeMovableBlockPositions;
            for (int i = 0; i < safeMovementPosition.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[safeMovementPosition[i].row_ID, safeMovementPosition[i].col_ID];
                block.HighlightNextMoveBlock();
                highlightedBlocks.Add(block);
            }

            List<BoardPosition> movementPosition = clickedPiece.movableBlockPositions;
            for (int i = 0; i < movementPosition.Count; i++)
            {
                Block block = ServiceLocator.Get<GameplayController>().board[movementPosition[i].row_ID, movementPosition[i].col_ID];
                block.HighlightNextMoveBlock();
                highlightedBlocks.Add(block);
            }
        }
    }
}
