using System.Collections.Generic;

public class CaptureSequence
{
    public List<BoardPosition> Landings = new();
    public List<BoardPosition> Captured = new();

    public int Length => Captured.Count;
}

public class MoveGenerator : Service<MoveGenerator>
{
    // The four diagonal directions a piece can move in: down-left, down-right, up-left, up-right.
    private static readonly (int dRow, int dCol)[] DiagonalDirections =
    {
        (1, -1), (1, 1), (-1, -1), (-1, 1)
    };

    private IRuleSet ruleSet;

    public void Initialize(IRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
    }

    private int[,] Occupancy => ServiceLocator.Get<GameplayController>().occupancy;
    private Piece[,] Pieces => ServiceLocator.Get<GameplayController>().pieces;

    // Player 2 (white) advances down the board, player 1 (black) advances up it.
    private static int ForwardDirection(int playerID)
    {
        return playerID == 2 ? 1 : -1;
    }

    // Non-king pieces may only make quiet moves in their forward direction; kings move in all four.
    private IEnumerable<(int dRow, int dCol)> GetMoveDirections(Piece piece)
    {
        int forward = ForwardDirection(piece.Player_ID);
        foreach ((int dRow, int dCol) dir in DiagonalDirections)
        {
            if (piece.IsCrownedKing || dir.dRow == forward)
            {
                yield return dir;
            }
        }
    }

    // Capture directions can differ from quiet-move directions: under some rulesets a non-king man
    // that can only ever *move* forward is still allowed to *capture* backward.
    private IEnumerable<(int dRow, int dCol)> GetCaptureDirections(Piece piece)
    {
        if (piece.IsCrownedKing || ruleSet.MenCaptureBackward)
        {
            return DiagonalDirections;
        }
        return GetMoveDirections(piece);
    }

    private List<Piece> GetPiecesForPlayer(int playerID)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        return playerID == 1 ? gameplayController.blackPieces : gameplayController.whitePieces;
    }

    public bool CanMove(int playerNumber)
    {
        List<Piece> pieces = GetPiecesForPlayer(playerNumber);
        for (int i = 0; i < pieces.Count; i++)
        {
            if (CanPieceMove(pieces[i]))
            {
                return true;
            }
        }
        return false;
    }

    public bool CanPieceMove(Piece piece)
    {
        return HasQuietMove(piece) || CanPieceKill(piece);
    }

    //AI
    private bool HasQuietMove(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;

        // Only the immediately adjacent square needs checking here, regardless of flying kings -
        // if it's empty that's already a valid destination, whether or not the king could also
        // slide further; enumerating every reachable square is SetAdjacentMovePosition's job, not
        // this yes/no check's.
        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            int adjRow = row + dir.dRow;
            int adjCol = col + dir.dCol;
            if (IsValidPosition(adjRow, adjCol) && Occupancy[adjRow, adjCol] == 0)
            {
                return true;
            }
        }
        return false;
    }

    public bool CanPieceKill(Piece piece)
    {
        return FindCaptureSequences(piece).Count > 0;
    }

    //AI
    public void CheckMovablePieces(int playerID, List<Piece> movablePieces)
    {
        List<Piece> pieces = GetPiecesForPlayer(playerID);

        // Under mandatory-maximum-capture, only pieces that can play the longest available
        // capture this turn are selectable at all - the player can't choose a piece with only a
        // shorter capture (or only a quiet move) while a longer capture exists elsewhere.
        if (ruleSet.MustCaptureMaximum)
        {
            int maxLength = GetMaxCaptureLength(playerID);
            if (maxLength > 0)
            {
                for (int i = 0; i < pieces.Count; i++)
                {
                    List<CaptureSequence> sequences = FindCaptureSequences(pieces[i]);
                    for (int j = 0; j < sequences.Count; j++)
                    {
                        if (sequences[j].Length == maxLength)
                        {
                            movablePieces.Add(pieces[i]);
                            break;
                        }
                    }
                }
                return;
            }
        }

        for (int i = 0; i < pieces.Count; i++)
        {
            if (CanPieceMove(pieces[i]))
            {
                movablePieces.Add(pieces[i]);
            }
        }
    }

    private int GetMaxCaptureLength(int playerID)
    {
        int maxLength = 0;
        List<Piece> pieces = GetPiecesForPlayer(playerID);
        for (int i = 0; i < pieces.Count; i++)
        {
            List<CaptureSequence> sequences = FindCaptureSequences(pieces[i]);
            for (int j = 0; j < sequences.Count; j++)
            {
                if (sequences[j].Length > maxLength)
                {
                    maxLength = sequences[j].Length;
                }
            }
        }
        return maxLength;
    }

    public void SetPiecePosition(Piece piece)
    {
        SetAdjacentMovePosition(piece);
        piece.captureSequences = GetLegalCaptures(piece);
    }

    private void SetAdjacentMovePosition(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;
        bool flying = piece.IsCrownedKing && ruleSet.FlyingKings;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(piece))
        {
            int targetRow = row + dir.dRow;
            int targetCol = col + dir.dCol;

            // A flying king can land on any empty square along the ray, not just the adjacent one.
            while (IsValidPosition(targetRow, targetCol) && Occupancy[targetRow, targetCol] == 0)
            {
                piece.movablePositions.Add(new BoardPosition(targetRow, targetCol));

                if (!flying) { break; }

                targetRow += dir.dRow;
                targetCol += dir.dCol;
            }
        }
    }

    // Every maximal capture sequence reachable from the piece's current position, of any length.
    public List<CaptureSequence> FindCaptureSequences(Piece piece)
    {
        List<CaptureSequence> results = new();
        SearchCaptures(piece, new List<BoardPosition>(), new List<BoardPosition>(), results);
        return results;
    }

    // Applies the active ruleset's mandatory-maximum-capture rule (if any) on top of
    // FindCaptureSequences, restricting the piece's own options to only its longest available
    // sequences when some *other* piece of the same player has an even longer one available.
    public List<CaptureSequence> GetLegalCaptures(Piece piece)
    {
        List<CaptureSequence> sequences = FindCaptureSequences(piece);

        if (!ruleSet.MustCaptureMaximum || sequences.Count == 0)
        {
            return sequences;
        }

        int maxLength = GetMaxCaptureLength(piece.Player_ID);
        return sequences.FindAll(s => s.Length == maxLength);
    }

    // Once already mid-chain with a specific piece (see Player.ContinueAfterKill), the player can
    // no longer switch pieces, so mandatory-maximum-capture is enforced by comparing only against
    // this piece's *own* remaining continuations, not the other pieces GetLegalCaptures also
    // checks - which sequence was "best" for piece selection was already decided when the chain
    // started. Filtering to the longest remaining continuation at every hop still reconstructs an
    // overall-longest sequence: if a shorter branch existed further along, prepending the hops
    // already taken would describe a strictly shorter total than the sequence a piece was
    // originally chosen for, which can't be true of the current longest-known sequence.
    public List<CaptureSequence> GetLegalContinuations(Piece piece)
    {
        List<CaptureSequence> sequences = FindCaptureSequences(piece);

        if (!ruleSet.MustCaptureMaximum || sequences.Count == 0)
        {
            return sequences;
        }

        int maxLength = 0;
        for (int i = 0; i < sequences.Count; i++)
        {
            if (sequences[i].Length > maxLength)
            {
                maxLength = sequences[i].Length;
            }
        }

        return sequences.FindAll(s => s.Length == maxLength);
    }

    private void SearchCaptures(Piece piece, List<BoardPosition> capturedSoFar, List<BoardPosition> landingsSoFar, List<CaptureSequence> results)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        int fromRow = piece.Row_ID;
        int fromCol = piece.Coloum_ID;
        bool foundFurtherCapture = false;
        bool flying = piece.IsCrownedKing && ruleSet.FlyingKings;

        foreach ((int dRow, int dCol) dir in GetCaptureDirections(piece))
        {
            // Walk outward to find a piece to potentially capture - a flying king may cross
            // several empty squares first; everyone else only ever looks at the adjacent square.
            int middleRow = fromRow + dir.dRow;
            int middleCol = fromCol + dir.dCol;

            if (flying)
            {
                while (IsValidPosition(middleRow, middleCol) && Occupancy[middleRow, middleCol] == 0)
                {
                    middleRow += dir.dRow;
                    middleCol += dir.dCol;
                }
            }

            if (!IsValidPosition(middleRow, middleCol) || Occupancy[middleRow, middleCol] == 0) { continue; }

            Piece middlePiece = Pieces[middleRow, middleCol];
            if (middlePiece.Player_ID == piece.Player_ID) { continue; }

            BoardPosition middlePos = new BoardPosition(middleRow, middleCol);
            if (capturedSoFar.Contains(middlePos)) { continue; } // already captured earlier in this chain

            // Enumerate landing squares beyond the captured piece: just the one immediately behind
            // it for a fixed-distance capture, or every empty square up to the next
            // obstruction/edge for a flying king.
            int landingRow = middleRow + dir.dRow;
            int landingCol = middleCol + dir.dCol;

            while (IsValidPosition(landingRow, landingCol) && Occupancy[landingRow, landingCol] == 0)
            {
                foundFurtherCapture = true;
                BoardPosition landingPos = new BoardPosition(landingRow, landingCol);

                // Simulate the jump (reusing the same mutate-then-revert technique the old
                // single/double-kill checks used), then recurse to look for further jumps from the
                // new landing before reverting.
                gameplayController.SetSquare(middleRow, middleCol, null);
                gameplayController.SetSquare(fromRow, fromCol, null);
                gameplayController.SetSquare(landingRow, landingCol, piece);

                capturedSoFar.Add(middlePos);
                landingsSoFar.Add(landingPos);

                SearchCaptures(piece, capturedSoFar, landingsSoFar, results);

                capturedSoFar.RemoveAt(capturedSoFar.Count - 1);
                landingsSoFar.RemoveAt(landingsSoFar.Count - 1);

                gameplayController.SetSquare(landingRow, landingCol, null);
                gameplayController.SetSquare(fromRow, fromCol, piece);
                gameplayController.SetSquare(middleRow, middleCol, middlePiece);

                if (!flying) { break; } // fixed-distance capture only ever has exactly one landing square

                landingRow += dir.dRow;
                landingCol += dir.dCol;
            }
        }

        if (!foundFurtherCapture && capturedSoFar.Count > 0)
        {
            results.Add(new CaptureSequence
            {
                Landings = new List<BoardPosition>(landingsSoFar),
                Captured = new List<BoardPosition>(capturedSoFar)
            });
        }
    }

    // Whether the piece ends up safe after playing out the *entire* given sequence (captured
    // pieces removed, piece moved to the final landing) - generalizes the old single/double-hop
    // safety checks to a sequence of any length.
    public bool IsSequenceSafe(Piece piece, CaptureSequence sequence)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        int originalRow = piece.Row_ID;
        int originalCol = piece.Coloum_ID;

        List<Piece> removedPieces = new();
        for (int i = 0; i < sequence.Captured.Count; i++)
        {
            BoardPosition captured = sequence.Captured[i];
            removedPieces.Add(Pieces[captured.row_ID, captured.col_ID]);
            gameplayController.SetSquare(captured.row_ID, captured.col_ID, null);
        }

        gameplayController.SetSquare(originalRow, originalCol, null);

        BoardPosition finalLanding = sequence.Landings[sequence.Landings.Count - 1];
        gameplayController.SetSquare(finalLanding.row_ID, finalLanding.col_ID, piece);

        bool isSafe = IsSafe(piece);

        gameplayController.SetSquare(finalLanding.row_ID, finalLanding.col_ID, null);
        gameplayController.SetSquare(originalRow, originalCol, piece);

        for (int i = 0; i < sequence.Captured.Count; i++)
        {
            BoardPosition captured = sequence.Captured[i];
            gameplayController.SetSquare(captured.row_ID, captured.col_ID, removedPieces[i]);
        }

        return isSafe;
    }

    // Public entry point for AI evaluation - same check IsSafeToMove/IsSequenceSafe use internally,
    // just against the piece's current position rather than a hypothetical one.
    public bool IsPieceSafe(Piece piece)
    {
        return IsSafe(piece);
    }

    // A piece is unsafe if an adjacent enemy piece could jump over it to the opposite square. This
    // only ever checks an immediate adjacent threat (not a flying king bearing down from a
    // distance) - a deliberate simplification of the "safe" heuristic used for move ordering, not
    // a rule-legality check, so it doesn't need to be exhaustive.
    // The enemy can do so unconditionally from its own forward direction, or from its backward
    // direction only if it's a king.
    private bool IsSafe(Piece piece)
    {
        int row = piece.Row_ID;
        int col = piece.Coloum_ID;
        int playerID = piece.Player_ID;

        if (col == 0 || col == ruleSet.Columns - 1 || row == 0 || row == ruleSet.Rows - 1)
        {
            /* safe position */
            return true;
        }

        int forward = ForwardDirection(playerID);

        foreach ((int dRow, int dCol) dir in DiagonalDirections)
        {
            int enemyRow = row + dir.dRow;
            int enemyCol = col + dir.dCol;

            if (!IsValidPosition(enemyRow, enemyCol) || Occupancy[enemyRow, enemyCol] == 0)
            {
                continue;
            }

            Piece enemy = Pieces[enemyRow, enemyCol];
            if (enemy.Player_ID == playerID)
            {
                continue;
            }

            bool enemyMovingBackward = dir.dRow != forward;
            if (enemyMovingBackward && !enemy.IsCrownedKing)
            {
                continue;
            }

            int landingRow = row - dir.dRow;
            int landingCol = col - dir.dCol;

            if (IsValidPosition(landingRow, landingCol) && Occupancy[landingRow, landingCol] == 0)
            {
                /* not safe position */
                return false;
            }
        }

        return true;
    }

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < ruleSet.Rows && col >= 0 && col < ruleSet.Columns;
    }

}
