using System.Collections.Generic;

public class CaptureSequence
{
    public List<BoardPosition> Landings = new();
    public List<BoardPosition> Captured = new();
    public int CapturedKingCount;

    // Index into Captured of the first King captured in this sequence, or int.MaxValue if it
    // captures no Kings at all - lower is "sooner" for the "First Blood" tiebreak (Italian).
    public int FirstKingCaptureIndex = int.MaxValue;

    public int Length => Captured.Count;
}

public class MoveGenerator : Service<MoveGenerator>
{
    // The four diagonal directions a piece can move in: down-left, down-right, up-left, up-right.
    private static readonly (int dRow, int dCol)[] DiagonalDirections =
    {
        (1, -1), (1, 1), (-1, -1), (-1, 1)
    };

    // The four orthogonal directions (Turkish dama): down, up, left, right.
    private static readonly (int dRow, int dCol)[] OrthogonalDirections =
    {
        (1, 0), (-1, 0), (0, -1), (0, 1)
    };

    private IRuleSet ruleSet;

    public void Initialize(IRuleSet ruleSet)
    {
        this.ruleSet = ruleSet;
    }

    private int[,] Occupancy => ServiceLocator.Get<GameplayController>().occupancy;
    private Piece[,] Pieces => ServiceLocator.Get<GameplayController>().pieces;

    private (int dRow, int dCol)[] Directions =>
        ruleSet.MovementScheme == MovementScheme.Orthogonal ? OrthogonalDirections : DiagonalDirections;

    // Player 2 (white) advances down the board, player 1 (black) advances up it.
    private static int ForwardDirection(int playerID)
    {
        return playerID == 2 ? 1 : -1;
    }

    // Non-king pieces may move in every direction except straight backward (for Diagonal that
    // leaves just the two forward diagonals; for Orthogonal it leaves forward plus both
    // sideways directions); kings move in all four.
    private IEnumerable<(int dRow, int dCol)> GetMoveDirections(Piece piece)
    {
        int forward = ForwardDirection(piece.Player_ID);
        foreach ((int dRow, int dCol) dir in Directions)
        {
            if (piece.IsCrownedKing || dir.dRow != -forward)
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
            return Directions;
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

    // One piece paired with one of its own candidate capture sequences - the unit the mandatory-
    // capture tiebreak cascade filters down, since some tiers (e.g. PreferKingMover) compare across
    // different pieces, not just within one piece's own sequence list.
    private struct CaptureCandidate
    {
        public Piece Piece;
        public CaptureSequence Sequence;
    }

    private List<CaptureCandidate> GetCaptureCandidates(int playerID)
    {
        List<Piece> pieces = GetPiecesForPlayer(playerID);
        List<CaptureCandidate> candidates = new();
        for (int i = 0; i < pieces.Count; i++)
        {
            List<CaptureSequence> sequences = FindCaptureSequences(pieces[i]);
            for (int j = 0; j < sequences.Count; j++)
            {
                candidates.Add(new CaptureCandidate { Piece = pieces[i], Sequence = sequences[j] });
            }
        }
        return candidates;
    }

    // Applies the ruleset's cascading mandatory-capture tiebreak tiers, in strict priority order, to
    // narrow a candidate list down to only the sequences actually legal to play. Any tier the
    // ruleset doesn't use is skipped entirely. Order: most pieces captured (MustCaptureMaximum),
    // then a King's sequence beats a man's (PreferKingMover, Italian "King Dominance"), then the
    // most Kings captured (PreferKingCaptures, Brazilian/Italian "Target Kings"), then whichever
    // captures its first King soonest (PreferEarlierKingCapture, Italian "First Blood").
    private List<CaptureCandidate> ApplyMandatoryCaptureTiers(List<CaptureCandidate> candidates)
    {
        if (candidates.Count == 0 || !ruleSet.MustCaptureMaximum) { return candidates; }

        int maxLength = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Sequence.Length > maxLength) { maxLength = candidates[i].Sequence.Length; }
        }
        candidates = candidates.FindAll(c => c.Sequence.Length == maxLength);

        if (ruleSet.PreferKingMover && candidates.Exists(c => c.Piece.IsCrownedKing))
        {
            candidates = candidates.FindAll(c => c.Piece.IsCrownedKing);
        }

        if (ruleSet.PreferKingCaptures)
        {
            int maxKingCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Sequence.CapturedKingCount > maxKingCount) { maxKingCount = candidates[i].Sequence.CapturedKingCount; }
            }
            candidates = candidates.FindAll(c => c.Sequence.CapturedKingCount == maxKingCount);
        }

        if (ruleSet.PreferEarlierKingCapture)
        {
            int minFirstKingIndex = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Sequence.FirstKingCaptureIndex < minFirstKingIndex) { minFirstKingIndex = candidates[i].Sequence.FirstKingCaptureIndex; }
            }
            candidates = candidates.FindAll(c => c.Sequence.FirstKingCaptureIndex == minFirstKingIndex);
        }

        return candidates;
    }

    //AI
    // Capturing is always mandatory: if any capture is available anywhere on the board, only
    // pieces with a qualifying capture (per ApplyMandatoryCaptureTiers) are selectable at all this
    // turn, quiet moves aren't offered to anyone (not even a piece that also happens to have a
    // quiet move of its own); otherwise any capture qualifies.
    public void CheckMovablePieces(int playerID, List<Piece> movablePieces)
    {
        List<CaptureCandidate> allCandidates = GetCaptureCandidates(playerID);

        if (allCandidates.Count > 0)
        {
            List<CaptureCandidate> qualifying = ApplyMandatoryCaptureTiers(allCandidates);
            for (int i = 0; i < qualifying.Count; i++)
            {
                if (!movablePieces.Contains(qualifying[i].Piece))
                {
                    movablePieces.Add(qualifying[i].Piece);
                }
            }
            return;
        }

        List<Piece> pieces = GetPiecesForPlayer(playerID);
        for (int i = 0; i < pieces.Count; i++)
        {
            if (CanPieceMove(pieces[i]))
            {
                movablePieces.Add(pieces[i]);
            }
        }
    }

    public void SetPiecePosition(Piece piece)
    {
        piece.captureSequences = GetLegalCaptures(piece);

        // A capture always takes priority over a quiet move for this piece - quiet-move squares
        // are only ever offered when it has no legal capture of its own to play instead.
        if (piece.captureSequences.Count == 0)
        {
            SetAdjacentMovePosition(piece);
        }
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
        SearchCaptures(piece, new List<BoardPosition>(), new List<bool>(), new List<BoardPosition>(), null, results);
        return results;
    }

    // Applies the ruleset's cascading mandatory-capture tiebreak tiers (see
    // ApplyMandatoryCaptureTiers) on top of FindCaptureSequences, restricting the piece's own
    // options to only its sequences that still qualify once every other piece of the same player
    // is taken into account too.
    public List<CaptureSequence> GetLegalCaptures(Piece piece)
    {
        List<CaptureSequence> sequences = FindCaptureSequences(piece);
        if (sequences.Count == 0) { return sequences; }

        List<CaptureCandidate> allCandidates = GetCaptureCandidates(piece.Player_ID);
        List<CaptureCandidate> qualifying = ApplyMandatoryCaptureTiers(allCandidates);

        List<CaptureSequence> result = new();
        for (int i = 0; i < qualifying.Count; i++)
        {
            if (qualifying[i].Piece == piece) { result.Add(qualifying[i].Sequence); }
        }
        return result;
    }

    // Once already mid-chain with a specific piece (see Player.ContinueAfterKill), the player can
    // no longer switch pieces, so every tiebreak tier is enforced by comparing only against this
    // piece's *own* remaining continuations, not the other pieces GetLegalCaptures also checks -
    // which sequence was "best" for piece selection was already decided when the chain started.
    // Filtering to the best remaining continuation at every hop still reconstructs an overall-best
    // sequence: if a worse branch existed further along, prepending the hops already taken would
    // describe a strictly worse total than the sequence a piece was originally chosen for, which
    // can't be true of the current best-known sequence. (PreferKingMover is a no-op here since
    // every candidate shares the same mover.)
    public List<CaptureSequence> GetLegalContinuations(Piece piece)
    {
        List<CaptureSequence> sequences = FindCaptureSequences(piece);
        if (sequences.Count == 0) { return sequences; }

        List<CaptureCandidate> candidates = new();
        for (int i = 0; i < sequences.Count; i++)
        {
            candidates.Add(new CaptureCandidate { Piece = piece, Sequence = sequences[i] });
        }

        List<CaptureCandidate> qualifying = ApplyMandatoryCaptureTiers(candidates);
        List<CaptureSequence> result = new();
        for (int i = 0; i < qualifying.Count; i++)
        {
            result.Add(qualifying[i].Sequence);
        }
        return result;
    }

    private void SearchCaptures(Piece piece, List<BoardPosition> capturedSoFar, List<bool> capturedWasKing, List<BoardPosition> landingsSoFar, (int dRow, int dCol)? lastDirection, List<CaptureSequence> results)
    {
        GameplayController gameplayController = ServiceLocator.Get<GameplayController>();
        int fromRow = piece.Row_ID;
        int fromCol = piece.Coloum_ID;
        bool foundFurtherCapture = false;
        bool flying = piece.IsCrownedKing && ruleSet.FlyingKings;

        foreach ((int dRow, int dCol) dir in GetCaptureDirections(piece))
        {
            // The "no 180-degree turn" rule (Turkish): can't immediately reverse back through the
            // square this same piece just left on the previous hop.
            if (ruleSet.ForbidImmediateReversal && lastDirection.HasValue
                && dir.dRow == -lastDirection.Value.dRow && dir.dCol == -lastDirection.Value.dCol)
            {
                continue;
            }

            // Walk outward to find a piece to potentially capture - a flying king may cross
            // several empty squares first; everyone else only ever looks at the adjacent square.
            int middleRow = fromRow + dir.dRow;
            int middleCol = fromCol + dir.dCol;

            if (flying)
            {
                while (IsPassable(middleRow, middleCol, capturedSoFar))
                {
                    middleRow += dir.dRow;
                    middleCol += dir.dCol;
                }
            }

            if (!IsValidPosition(middleRow, middleCol) || Occupancy[middleRow, middleCol] == 0) { continue; }

            Piece middlePiece = Pieces[middleRow, middleCol];
            if (middlePiece.Player_ID == piece.Player_ID) { continue; }

            // Already captured earlier this same live-play turn (DeferCaptureRemoval): it's still
            // sitting on the board as an obstacle, but it's dead and can't be jumped a second time.
            if (middlePiece.IsCaptured) { continue; }

            // Immunity (Italian): a man can never capture a King - only an opposing King may.
            if (ruleSet.MenCannotCaptureKings && !piece.IsCrownedKing && middlePiece.IsCrownedKing) { continue; }

            BoardPosition middlePos = new BoardPosition(middleRow, middleCol);
            if (capturedSoFar.Contains(middlePos)) { continue; } // already captured earlier in this chain

            // Enumerate landing squares beyond the captured piece: just the one immediately behind
            // it for a fixed-distance capture, or every empty square up to the next
            // obstruction/edge for a flying king.
            int landingRow = middleRow + dir.dRow;
            int landingCol = middleCol + dir.dCol;

            while (IsPassable(landingRow, landingCol, capturedSoFar))
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
                capturedWasKing.Add(middlePiece.IsCrownedKing);
                landingsSoFar.Add(landingPos);

                SearchCaptures(piece, capturedSoFar, capturedWasKing, landingsSoFar, dir, results);

                capturedSoFar.RemoveAt(capturedSoFar.Count - 1);
                capturedWasKing.RemoveAt(capturedWasKing.Count - 1);
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
            int kingCount = 0;
            int firstKingIndex = int.MaxValue;
            for (int i = 0; i < capturedWasKing.Count; i++)
            {
                if (capturedWasKing[i])
                {
                    kingCount++;
                    if (firstKingIndex == int.MaxValue) { firstKingIndex = i; }
                }
            }

            results.Add(new CaptureSequence
            {
                Landings = new List<BoardPosition>(landingsSoFar),
                Captured = new List<BoardPosition>(capturedSoFar),
                CapturedKingCount = kingCount,
                FirstKingCaptureIndex = firstKingIndex
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

        foreach ((int dRow, int dCol) dir in Directions)
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

            // Already captured earlier this turn (DeferCaptureRemoval) - still on the board as an
            // obstacle, but dead, so it poses no threat.
            if (enemy.IsCaptured)
            {
                continue;
            }

            // Immunity (Italian): a man threatens nothing if piece is a King - only an opposing
            // King could actually capture it.
            if (ruleSet.MenCannotCaptureKings && piece.IsCrownedKing && !enemy.IsCrownedKing)
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

    // A square is passable for a flying king's outward search / landing enumeration if it's
    // genuinely empty. Under DeferCaptureRemoval rulesets, a piece captured earlier this turn
    // stays on the board (blocking the line) until the whole turn ends - even though
    // SearchCaptures' own mutate-then-revert simulation has already cleared it from the live
    // occupancy grid for bookkeeping purposes - so those squares are excluded too. Other rulesets
    // remove a captured piece immediately, which can open up new paths mid-turn, so no such
    // exclusion applies there.
    private bool IsPassable(int row, int col, List<BoardPosition> capturedSoFar)
    {
        if (!IsValidPosition(row, col) || Occupancy[row, col] != 0) { return false; }
        return !ruleSet.DeferCaptureRemoval || !capturedSoFar.Contains(new BoardPosition(row, col));
    }

}
