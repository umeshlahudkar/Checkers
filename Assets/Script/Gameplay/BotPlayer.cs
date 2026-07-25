using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        protected override void ContinueAfterKill(Piece selectedPiece)
        {
            selectedPiece.ResetAllList();
            List<CaptureSequence> sequences = ServiceLocator.Get<MoveGenerator>().GetLegalContinuations(selectedPiece);

            CaptureSequence chosen = ChooseSafestOrFirst(selectedPiece, sequences);
            BoardPosition position = chosen.Landings[0];

            Block b = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];
            b.IsNextToNextHighlighted = true;
            b.CapturedPosition = chosen.Captured[0];
            OnHighlightedTargetBlockClick(b);
            ResetNextToNextHighlightedBlock();
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
            Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

            OnHighlightedTargetBlockClick(block);
        }
    }
}
