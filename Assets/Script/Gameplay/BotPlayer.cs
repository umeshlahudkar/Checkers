using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Gameplay
{
    public class BotPlayer : Player
    {
        private readonly WaitForSeconds waitForSeconds = new(1f);

        // The full multi-hop sequence BotMinimax's search actually scored, so later hops can be
        // played exactly as searched instead of independently re-derived hop-by-hop by
        // ChooseSafestOrFirst's much shallower heuristic (which has no concept of the sequence
        // length or quality the search evaluated). Null whenever there's no capture chain in
        // flight - a quiet move, or between turns. pendingSequenceHopIndex is which hop of it is
        // next; hop 0 is always played directly by MakeMove, so it starts at 1.
        private CaptureSequence pendingSequence;
        private int pendingSequenceHopIndex;

        protected override void OnTurnReady()
        {
            StartCoroutine(PlayAITurn());
        }

        private IEnumerator PlayAITurn()
        {
            yield return waitForSeconds;

            BotAISettingsSO settings = ServiceLocator.Get<GameManager>().BotAISettings;
            int depth = ServiceLocator.Get<GameManager>().BotDifficulty switch
            {
                BotDifficulty.Hard => settings.hardDepth,
                BotDifficulty.Medium => settings.mediumDepth,
                _ => settings.easyDepth,
            };

            // The search runs on a background thread (see BotMinimax.StartSearch) - poll instead of
            // blocking so Hard's deeper search doesn't stall the main thread for a frame.
            Task<AIMoveOption?> task = BotMinimax.StartSearch(Player_ID, depth);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
                yield break;
            }

            // The turn can move on while this search was still running in the background (e.g. a
            // timeout) - applying a stale move at that point would act out of turn.
            if (!IsMyTurn) { yield break; }

            BotMinimax.AIMove? bestMove = BotMinimax.ResolveMove(task.Result);
            if (!bestMove.HasValue) { yield break; }

            BotMinimax.AIMove move = bestMove.Value;
            if (move.Sequence != null)
            {
                MakeMove(move.Piece, move.Sequence);
            }
            else
            {
                MakeMove(move.Piece, move.Position);
            }
        }

        protected override void ContinueAfterKill(Piece selectedPiece, BoardPosition lastCapturedPosition)
        {
            selectedPiece.ResetAllList();

            // Same ForbidImmediateReversal direction derivation as HumanPlayer.ContinueAfterKill -
            // see there for why the captured square, not a stored direction, is what's threaded
            // through.
            (int dRow, int dCol) lastDirection = (
                System.Math.Sign(selectedPiece.Row_ID - lastCapturedPosition.row_ID),
                System.Math.Sign(selectedPiece.Coloum_ID - lastCapturedPosition.col_ID));
            List<CaptureSequence> sequences = ServiceLocator.Get<MoveGenerator>().GetLegalContinuations(selectedPiece, lastDirection);

            CaptureSequence chosen = ChoosePlannedOrSafestOrFirst(selectedPiece, sequences);
            BoardPosition position = chosen.Landings[0];

            Block b = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
            b.IsNextToNextHighlighted = true;
            b.CapturedPosition = chosen.Captured[0];
            OnHighlightedTargetBlockClick(b);
            ResetNextToNextHighlightedBlock();
        }

        // Prefers replaying the next hop of pendingSequence - the sequence BotMinimax's search
        // actually chose - over ChooseSafestOrFirst's much shallower per-hop heuristic. Falls back
        // to that heuristic (and abandons the plan for the rest of this chain) if the plan's next
        // hop no longer matches any currently-legal continuation - defensively, since nothing in
        // this codebase's synchronous single-turn execution should actually invalidate it mid-chain,
        // but a model/reality divergence elsewhere (e.g. a rule the AI's search doesn't model)
        // shouldn't be allowed to make the bot commit to a hop that isn't actually legal.
        private CaptureSequence ChoosePlannedOrSafestOrFirst(Piece piece, List<CaptureSequence> sequences)
        {
            if (pendingSequence != null && pendingSequenceHopIndex < pendingSequence.Length)
            {
                BoardPosition plannedLanding = pendingSequence.Landings[pendingSequenceHopIndex];
                BoardPosition plannedCapture = pendingSequence.Captured[pendingSequenceHopIndex];

                for (int i = 0; i < sequences.Count; i++)
                {
                    BoardPosition landing = sequences[i].Landings[0];
                    BoardPosition captured = sequences[i].Captured[0];
                    if (landing.row_ID == plannedLanding.row_ID && landing.col_ID == plannedLanding.col_ID
                        && captured.row_ID == plannedCapture.row_ID && captured.col_ID == plannedCapture.col_ID)
                    {
                        pendingSequenceHopIndex++;
                        return sequences[i];
                    }
                }
            }

            pendingSequence = null;
            return ChooseSafestOrFirst(piece, sequences);
        }

        private CaptureSequence ChooseSafestOrFirst(Piece piece, List<CaptureSequence> sequences)
        {
            MoveGenerator moveGenerator = ServiceLocator.Get<MoveGenerator>();
            for (int i = 0; i < sequences.Count; i++)
            {
                if (moveGenerator.IsSequenceSafe(piece, sequences[i]))
                {
                    return sequences[i];
                }
            }
            return sequences[0];
        }

        private void MakeMove(Piece piece, CaptureSequence sequence)
        {
            SelectPieceForNewMove(piece);

            // Hop 0 is played directly below; if the search's chosen sequence has more hops than
            // that, ContinueAfterKill's ChoosePlannedOrSafestOrFirst picks up from index 1.
            pendingSequence = sequence;
            pendingSequenceHopIndex = 1;

            BoardPosition landing = sequence.Landings[0];
            Block block = ServiceLocator.Get<GameplayController>().board[landing.row_ID, landing.col_ID];

            block.IsNextToNextHighlighted = true;
            block.CapturedPosition = sequence.Captured[0];
            nextToNexthighlightedBlocks.Add(block);

            OnHighlightedTargetBlockClick(block);
        }

        private void MakeMove(Piece piece, BoardPosition position)
        {
            SelectPieceForNewMove(piece);
            pendingSequence = null; // a quiet move, not a capture chain - nothing to plan ahead
            Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

            OnHighlightedTargetBlockClick(block);
        }
    }
}
