using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Depth-limited minimax with alpha-beta pruning for BotPlayer. The search itself runs entirely
// against BoardState's pure AICell[,] functions on a background thread (via StartSearch/Task.Run)
// so it never touches a live Piece/Block/ServiceLocator and never blocks the main thread - that's
// what used to freeze the game for a few frames on Hard difficulty. ResolveMove translates the
// winning move back into real Piece/CaptureSequence/BoardPosition objects on the main thread once
// the task completes.
public static class BotMinimax
{
    // Depth used for BotPlayer's Hard difficulty and for HumanPlayer's Hint, which is documented
    // to suggest the same "objectively best" move Hard would play regardless of match difficulty.
    public const int HardDepth = 4;

    public struct AIMove
    {
        public Piece Piece;
        public CaptureSequence Sequence; // null for a quiet move
        public BoardPosition Position;   // only meaningful when Sequence is null
    }

    // Snapshots the live board into plain data and starts the search on a background thread. Call
    // this on the main thread; poll the returned task's IsCompleted (e.g. from a coroutine) rather
    // than blocking on it.
    public static Task<AIMoveOption?> StartSearch(int playerID, int depth)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        IRuleSet ruleSet = ServiceLocator.Get<GameManager>().RuleSet;

        AIRules rules = new()
        {
            Rows = ruleSet.Rows,
            Columns = ruleSet.Columns,
            FlyingKings = ruleSet.FlyingKings,
            MenCaptureBackward = ruleSet.MenCaptureBackward,
            MustCaptureMaximum = ruleSet.MustCaptureMaximum
        };

        AICell[,] snapshot = new AICell[rules.Rows, rules.Columns];
        for (int r = 0; r < rules.Rows; r++)
        {
            for (int c = 0; c < rules.Columns; c++)
            {
                Piece piece = gameplayController.pieces[r, c];
                snapshot[r, c] = new AICell
                {
                    PlayerID = gameplayController.occupancy[r, c],
                    IsKing = piece != null && piece.IsCrownedKing
                };
            }
        }

        return Task.Run(() => GetBestMove(snapshot, rules, playerID, depth));
    }

    // Translates a completed search's plain-int result back into a real Piece/CaptureSequence/
    // BoardPosition. Must run on the main thread - reads the live GameplayController.pieces grid.
    public static AIMove? ResolveMove(AIMoveOption? option)
    {
        if (!option.HasValue) { return null; }

        AIMoveOption move = option.Value;
        Piece piece = ServiceLocator.Get<GameplayController>().pieces[move.FromRow, move.FromCol];

        if (move.Sequence != null)
        {
            CaptureSequence sequence = new();
            for (int i = 0; i < move.Sequence.Landings.Count; i++)
            {
                AIPosition landing = move.Sequence.Landings[i];
                sequence.Landings.Add(new BoardPosition(landing.Row, landing.Col));
            }
            for (int i = 0; i < move.Sequence.Captured.Count; i++)
            {
                AIPosition captured = move.Sequence.Captured[i];
                sequence.Captured.Add(new BoardPosition(captured.Row, captured.Col));
            }
            return new AIMove { Piece = piece, Sequence = sequence };
        }

        return new AIMove { Piece = piece, Position = new BoardPosition(move.Position.Row, move.Position.Col) };
    }

    private static AIMoveOption? GetBestMove(AICell[,] cells, AIRules rules, int playerID, int depth)
    {
        List<AIMoveOption> moves = BoardState.GetLegalMoves(cells, playerID, rules);
        if (moves.Count == 0) { return null; }

        int opponent = playerID == 1 ? 2 : 1;
        int bestScore = int.MinValue;
        AIMoveOption bestMove = moves[0];
        int alpha = int.MinValue;

        for (int i = 0; i < moves.Count; i++)
        {
            AIUndoInfo undo = BoardState.ApplyMove(cells, moves[i], rules);
            int score = Search(cells, rules, opponent, playerID, depth - 1, alpha, int.MaxValue);
            BoardState.UndoMove(cells, undo);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = moves[i];
            }
            if (score > alpha) { alpha = score; }
        }

        return bestMove;
    }

    // Alpha-beta pruned minimax - same result plain minimax would return (same best move, same
    // score), just skips branches that can't change it.
    private static int Search(AICell[,] cells, AIRules rules, int playerToMove, int aiPlayerID, int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            return EvaluateBoard(cells, rules, aiPlayerID);
        }

        List<AIMoveOption> moves = BoardState.GetLegalMoves(cells, playerToMove, rules);
        if (moves.Count == 0)
        {
            // No legal move: this side has lost the game from here.
            return playerToMove == aiPlayerID ? -10000 : 10000;
        }

        bool maximizing = playerToMove == aiPlayerID;
        int opponent = playerToMove == 1 ? 2 : 1;
        int best = maximizing ? int.MinValue : int.MaxValue;

        for (int i = 0; i < moves.Count; i++)
        {
            AIUndoInfo undo = BoardState.ApplyMove(cells, moves[i], rules);
            int score = Search(cells, rules, opponent, aiPlayerID, depth - 1, alpha, beta);
            BoardState.UndoMove(cells, undo);

            if (maximizing)
            {
                if (score > best) { best = score; }
                if (best > alpha) { alpha = best; }
            }
            else
            {
                if (score < best) { best = score; }
                if (best < beta) { beta = best; }
            }

            if (alpha >= beta) { break; } // opponent already has a better alternative elsewhere
        }

        return best;
    }

    // myScore - opponentScore for the board in its current (possibly simulated) state.
    private static int EvaluateBoard(AICell[,] cells, AIRules rules, int aiPlayerID)
    {
        int opponentPlayerID = aiPlayerID == 1 ? 2 : 1;
        int myScore = 0;
        int opponentScore = 0;

        for (int r = 0; r < rules.Rows; r++)
        {
            for (int c = 0; c < rules.Columns; c++)
            {
                AICell cell = cells[r, c];
                if (cell.PlayerID == 0) { continue; }

                int pieceScore = EvaluatePiece(cells, rules, r, c, cell);
                if (cell.PlayerID == aiPlayerID) { myScore += pieceScore; } else { opponentScore += pieceScore; }
            }
        }

        myScore += BoardState.GetLegalMoves(cells, aiPlayerID, rules).Count * 2;
        opponentScore += BoardState.GetLegalMoves(cells, opponentPlayerID, rules).Count * 2;

        return myScore - opponentScore;
    }

    private static int EvaluatePiece(AICell[,] cells, AIRules rules, int row, int col, AICell cell)
    {
        int score = cell.IsKing ? 180 : 100;

        if (!cell.IsKing)
        {
            int promotionRow = cell.PlayerID == 2 ? rules.Rows - 1 : 0;
            if (Mathf.Abs(row - promotionRow) <= 2)
            {
                score += 30; // near promotion
            }
        }

        if (BoardState.IsSafe(cells, row, col, rules))
        {
            score += 15; // protected
        }
        else
        {
            score -= 40; // vulnerable
        }

        bool inCenter = row >= 2 && row <= rules.Rows - 3 && col >= 2 && col <= rules.Columns - 3;
        if (inCenter)
        {
            score += 10;
        }

        return score;
    }
}
