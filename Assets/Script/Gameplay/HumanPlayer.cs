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
        protected override void ContinueAfterKill(Piece selectedPiece, BoardPosition lastCapturedPosition)
        {
            selectedPiece.ResetAllList();

            // The direction of the hop just played, so a ForbidImmediateReversal ruleset (Turkish)
            // can't offer a next hop that reverses straight back through the square just jumped -
            // the captured square and selectedPiece's current (post-hop) position are always
            // exactly one direction apart, regardless of how far a flying king actually traveled.
            (int dRow, int dCol) lastDirection = (
                System.Math.Sign(selectedPiece.Row_ID - lastCapturedPosition.row_ID),
                System.Math.Sign(selectedPiece.Coloum_ID - lastCapturedPosition.col_ID));
            selectedPiece.captureSequences = ServiceLocator.Get<MoveGenerator>().GetLegalContinuations(selectedPiece, lastDirection);

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
        // BotAISettingsSO.hardDepth) on a background thread, same as BotPlayer's own turn. Only
        // meaningful at the start of a turn, before a piece is selected (see
        // GamePage.RefreshHintUndoButtons, which gates the button to that same window) - and, unlike
        // that button-side gate, enforced here too rather than trusted to it alone.
        //
        // BotMinimax's search has no concept of "mid-chain" (that's Player-instance-only state, not
        // part of the board it searches), so a hint requested while IsChainInProgress could suggest
        // an unrelated piece/move entirely. Worse, ShowHintRoutine's own SelectPieceForNewMove call
        // below unconditionally resets chainCaptureCount and clears capturedThisChain - exactly the
        // same corruption OnHighlightedPieceClick's own IsChainInProgress guard exists to prevent for
        // a piece click, just reached through this method instead: any DeferCaptureRemoval piece
        // already marked-captured earlier this chain would be leaked (still on the board, still
        // shrunk, never actually destroyed) since nothing else still holds a reference to it once
        // capturedThisChain is cleared out from under it.
        public void ShowHint()
        {
            if (IsChainInProgress) { return; }

            StartCoroutine(ShowHintRoutine());
        }

        private IEnumerator ShowHintRoutine()
        {
            // Prevent a second click from starting an overlapping search while this one's still
            // running - RefreshHintUndoButtons (called below) re-enables it once we're done.
            ServiceLocator.Get<GamePageManager>().GamePage.SetHintUndoInteractable(false);

            // Hint takes over the display - clear every movable piece's pulsing highlight so only
            // the suggested piece/destination stand out once the search comes back.
            ResetHighlightedBlocks();
            int myGeneration = SelectionGeneration;

            int depth = ServiceLocator.Get<GameManager>().BotAISettings.hardDepth;
            Task<AIMoveOption?> task = BotMinimax.StartSearch(Player_ID, depth);
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

            // The player may have clicked a different piece (or the turn may have moved on, e.g. a
            // timeout) while this search was still running in the background - applying the hint at
            // that point would stomp on whatever's now shown, so bail without touching anything.
            if (!IsMyTurn || SelectionGeneration != myGeneration) { yield break; }

            BotMinimax.AIMove? bestMove = BotMinimax.ResolveMove(task.Result);
            if (!bestMove.HasValue)
            {
                HighlightMovablePieceBlock();
                yield break;
            }

            BotMinimax.AIMove move = bestMove.Value;
            BoardPosition landing = move.Sequence != null ? move.Sequence.Landings[0] : move.Position;

            GameplayController gameplayController = ServiceLocator.Get<GameplayController>();

            // The suggested piece gets the normal piece-highlight (same as any selectable/selected
            // piece) - only the destination gets the distinct hint styling.
            Block fromBlock = gameplayController.board[move.Piece.Row_ID, move.Piece.Coloum_ID];
            fromBlock.HighlightPieceBlock();
            highlightedBlocks.Add(fromBlock);

            gameplayController.ShowHintHighlight(landing.row_ID, landing.col_ID);

            // Also make the suggested destination directly clickable, wired exactly like a normal
            // move highlight (HighlightCaptureSequences/HighlightBlocks below) - selecting the piece
            // up front so a click there plays out through the same pipeline a manual pick would,
            // multi-hop capture chains included.
            SelectPieceForNewMove(move.Piece);

            Block targetBlock = gameplayController.board[landing.row_ID, landing.col_ID];
            bool isCapture = move.Sequence != null;
            targetBlock.MakeHintTargetClickable(isCapture);
            highlightedBlocks.Add(targetBlock);

            if (isCapture)
            {
                targetBlock.CapturedPosition = move.Sequence.Captured[0];
                nextToNexthighlightedBlocks.Add(targetBlock);
            }
        }

        public override void OnHighlightedPieceClick(Piece clickedPiece)
        {
            ServiceLocator.Get<GameplayController>().ClearHintHighlight();

            // Mid-chain, the only legal click is on one of selectedPiece's highlighted continuation
            // targets (handled by OnHighlightedTargetBlockClick, not here) - any piece click at all,
            // this method's only job, must not be allowed through to SelectPieceForNewMove below.
            // movablePieces is a start-of-turn snapshot and can still contain a different piece that
            // also had its own capture available, and falling through for THAT piece would silently
            // abandon the still-mandatory chain with an illegal partial capture left standing.
            // Falling through for selectedPiece itself (a re-click) is just as unsafe in a quieter
            // way - SelectPieceForNewMove would reset chainCaptureCount and clear capturedThisChain
            // without ever destroying whatever it was tracking, leaking those pieces the same way a
            // genuine abandoned chain would. Re-showing the same forced continuation instead of
            // touching any selection state handles both cases correctly with one check.
            if (IsChainInProgress)
            {
                ResetHighlightedBlocks();
                if (clickedPiece != selectedPiece)
                {
                    clickedPiece.PlayShakeAnimation();
                }
                ContinueAfterKill(selectedPiece, lastCapturedPosition);
                return;
            }

            ResetHighlightedBlocks();

            // Checking the turn's own movablePieces (computed once via CheckMovablePieces, which
            // already applies mandatory-capture) rather than re-deriving CanPieceMove for just this
            // piece - a piece with only a quiet move isn't selectable at all while some other piece
            // on the board has a mandatory capture available, even though it can technically move.
            if (movablePieces.Contains(clickedPiece))
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
