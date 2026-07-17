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

        // Hard: full priority order - a safe double-kill beats an unsafe one, which beats a safe
        // single kill, and so on down to a plain (unsafe) move. Optimal play.
        private void PlayHard()
        {
            if (TryBestOf(p => p.safeDoubleKillerBlockPositions, isKillMove: true)) { return; }
            if (TryBestOf(p => p.doubleKillerBlockPositions, isKillMove: true)) { return; }
            if (TryBestOf(p => p.safeKillerBlockPositions, isKillMove: true)) { return; }
            if (TryBestOf(p => p.killerBlockPositions, isKillMove: true)) { return; }
            if (TryBestOf(p => p.safeMovableBlockPositions, isKillMove: false)) { return; }
            TryBestOf(p => p.movableBlockPositions, isKillMove: false);
        }

        // Medium: still takes a kill over a move (and a double-kill over a single one), but is
        // indifferent to whether the resulting position is safe - it doesn't look further ahead.
        private void PlayMedium()
        {
            if (TryBestOf(p => Combine(p.safeDoubleKillerBlockPositions, p.doubleKillerBlockPositions), isKillMove: true)) { return; }
            if (TryBestOf(p => Combine(p.safeKillerBlockPositions, p.killerBlockPositions), isKillMove: true)) { return; }
            TryBestOf(p => Combine(p.safeMovableBlockPositions, p.movableBlockPositions), isKillMove: false);
        }

        // Easy: no priority at all - every legal destination across every piece is an equally
        // likely pick, so it can walk past a free kill without taking it.
        private void PlayEasy()
        {
            List<(Piece piece, BoardPosition position, bool isKillMove)> options = new();

            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                AddOptions(options, piece, piece.safeDoubleKillerBlockPositions, isKillMove: true);
                AddOptions(options, piece, piece.doubleKillerBlockPositions, isKillMove: true);
                AddOptions(options, piece, piece.safeKillerBlockPositions, isKillMove: true);
                AddOptions(options, piece, piece.killerBlockPositions, isKillMove: true);
                AddOptions(options, piece, piece.safeMovableBlockPositions, isKillMove: false);
                AddOptions(options, piece, piece.movableBlockPositions, isKillMove: false);
            }

            if (options.Count == 0) { return; }

            (Piece piece, BoardPosition position, bool isKillMove) choice = options[UnityEngine.Random.Range(0, options.Count)];
            MakeMove(choice.piece, choice.position, choice.isKillMove);
        }

        private static void AddOptions(List<(Piece piece, BoardPosition position, bool isKillMove)> options, Piece piece, List<BoardPosition> positions, bool isKillMove)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                options.Add((piece, positions[i], isKillMove));
            }
        }

        private static List<BoardPosition> Combine(List<BoardPosition> a, List<BoardPosition> b)
        {
            if (a.Count == 0) { return b; }
            if (b.Count == 0) { return a; }

            List<BoardPosition> combined = new(a);
            combined.AddRange(b);
            return combined;
        }

        private bool TryBestOf(Func<Piece, List<BoardPosition>> getPositions, bool isKillMove)
        {
            for (int i = 0; i < movablePieces.Count; i++)
            {
                Piece piece = movablePieces[i];
                List<BoardPosition> positions = getPositions(piece);
                if (positions.Count == 0) { continue; }

                MakeMove(piece, positions[0], isKillMove);
                return true;
            }
            return false;
        }

        private void MakeMove(Piece piece, BoardPosition position, bool isKillMove)
        {
            selectedPiece = piece;
            Block block = ServiceLocator.Get<GameplayController>().board[position.row_ID, position.col_ID];

            if (isKillMove)
            {
                block.IsNextToNextHighlighted = true;
                nextToNexthighlightedBlocks.Add(block);
            }

            OnHighlightedTargetBlockClick(block);
        }
    }
}
