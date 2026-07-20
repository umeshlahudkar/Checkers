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

        private void SetMovablePosition()
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                piece.ResetAllList();
                ServiceLocator.Get<MoveGenerator>().SetPiecePosition(piece);
            }
        }

        private void CheckPieceMove()
        {
            movablePieces.Shuffle();

            switch (ServiceLocator.Get<GameManager>().BotDifficulty)
            {
                case BotDifficulty.Hard:
                    PlayHard();
                    break;
                case BotDifficulty.Medium:
                    PlayMedium();
                    break;
                default:
                    PlayEasy();
                    break;
            }
        }

        // Hard: the longest available capture always wins (regardless of safety), and only among
        // captures/moves of that same priority does a safe option beat an unsafe one. Optimal play.
        private void PlayHard()
        {
            if (TryBestCapture(preferSafe: true)) { return; }
            if (TryBestMove(preferSafe: true)) { return; }
            TryBestMove(preferSafe: false);
        }

        // Medium: still takes the longest available capture over a move, but is indifferent to
        // whether the resulting position is safe - it doesn't look further ahead.
        private void PlayMedium()
        {
            if (TryBestCapture(preferSafe: null)) { return; }
            TryBestMove(preferSafe: null);
        }

        // Easy: no priority at all - every legal destination across every piece (captures of any
        // length included) is an equally likely pick, so it can walk past a free capture without
        // taking it.
        private void PlayEasy()
        {
            List<(Piece piece, CaptureSequence sequence)> captureOptions = new();
            List<(Piece piece, BoardPosition position)> moveOptions = new();

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                for (int j = 0; j < piece.captureSequences.Count; j++)
                {
                    captureOptions.Add((piece, piece.captureSequences[j]));
                }
                for (int j = 0; j < piece.movablePositions.Count; j++)
                {
                    moveOptions.Add((piece, piece.movablePositions[j]));
                }
            }

            int totalOptions = captureOptions.Count + moveOptions.Count;
            if (totalOptions == 0) { return; }

            int choiceIndex = UnityEngine.Random.Range(0, totalOptions);
            if (choiceIndex < captureOptions.Count)
            {
                (Piece piece, CaptureSequence sequence) choice = captureOptions[choiceIndex];
                MakeMove(choice.piece, choice.sequence);
            }
            else
            {
                (Piece piece, BoardPosition position) choice = moveOptions[choiceIndex - captureOptions.Count];
                MakeMove(choice.piece, choice.position);
            }
        }

        // Finds the longest capture available across all movable pieces, then (unless preferSafe is
        // null) prefers one that leaves the piece safe afterward, falling back to any at that same
        // longest length if none are safe.
        private bool TryBestCapture(bool? preferSafe)
        {
            MoveGenerator moveGenerator = ServiceLocator.Get<MoveGenerator>();

            int maxLength = 0;
            for (int i = 0; i < movablePieces.Count; i++)
            {
                List<CaptureSequence> sequences = movablePieces[i].captureSequences;
                for (int j = 0; j < sequences.Count; j++)
                {
                    if (sequences[j].Length > maxLength)
                    {
                        maxLength = sequences[j].Length;
                    }
                }
            }

            if (maxLength == 0) { return false; }

            (Piece piece, CaptureSequence sequence)? fallback = null;

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                List<CaptureSequence> sequences = piece.captureSequences;
                for (int j = 0; j < sequences.Count; j++)
                {
                    CaptureSequence sequence = sequences[j];
                    if (sequence.Length != maxLength) { continue; }

                    if (!preferSafe.HasValue || moveGenerator.IsSequenceSafe(piece, sequence) == preferSafe.Value)
                    {
                        MakeMove(piece, sequence);
                        return true;
                    }

                    fallback ??= (piece, sequence);
                }
            }

            if (fallback.HasValue)
            {
                MakeMove(fallback.Value.piece, fallback.Value.sequence);
                return true;
            }
            return false;
        }

        private bool TryBestMove(bool? preferSafe)
        {
            MoveGenerator moveGenerator = ServiceLocator.Get<MoveGenerator>();

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                for (int j = 0; j < piece.movablePositions.Count; j++)
                {
                    BoardPosition position = piece.movablePositions[j];

                    if (preferSafe.HasValue && moveGenerator.IsSafeToMove(piece, position.row_ID, position.col_ID) != preferSafe.Value)
                    {
                        continue;
                    }

                    MakeMove(piece, position);
                    return true;
                }
            }
            return false;
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
