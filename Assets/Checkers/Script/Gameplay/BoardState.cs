using System.Collections.Generic;

// One square's data for the AI's own board snapshot - built fresh from GameplayController's
// occupancy/pieces grids right before a bot search starts. Deliberately separate from those
// "gameplay logic" grids (which only ever need 0/1/2, see GameplayController.occupancy): the
// search additionally needs king status per square, which the live grids don't carry.
public struct AICell
{
    public int PlayerID; // 0 empty, 1, 2 - mirrors GameplayController.occupancy
    public bool IsKing;
}

public struct AIRules
{
    public int Rows;
    public int Columns;
    public MovementScheme MovementScheme;
    public bool FlyingKings;
    public bool MenCaptureBackward;
    public bool MenCannotCaptureKings;
    public bool MustCaptureMaximum;
    public bool PreferKingMover;
    public bool PreferKingCaptures;
    public bool PreferEarlierKingCapture;
    public bool DeferCaptureRemoval;
    public bool ForbidImmediateReversal;
}

// Plain-data copy of BotAISettingsSO's evaluation weights - snapshotted on the main thread (see
// BotMinimax.StartSearch) rather than read directly from the ScriptableObject during the search,
// since UnityEngine.Object access isn't safe off the main thread.
public struct AIWeights
{
    public int ManScore;
    public int KingScore;
    public int NearPromotionDistance;
    public int NearPromotionBonus;
    public int ProtectedBonus;
    public int VulnerablePenalty;
    public int CenterMargin;
    public int CenterBonus;
    public int MobilityWeight;
}

public struct AIPosition
{
    public int Row;
    public int Col;

    public AIPosition(int row, int col)
    {
        Row = row;
        Col = col;
    }
}

public class AICaptureSequence
{
    public List<AIPosition> Landings = new();
    public List<AIPosition> Captured = new();
    public int CapturedKingCount;

    // Mirrors CaptureSequence.FirstKingCaptureIndex.
    public int FirstKingCaptureIndex = int.MaxValue;

    public int Length => Captured.Count;
}

public struct AIMoveOption
{
    public int FromRow;
    public int FromCol;
    public AICaptureSequence Sequence; // null for a quiet move
    public AIPosition Position;        // only meaningful when Sequence is null
}

public struct AIUndoInfo
{
    public int FromRow;
    public int FromCol;
    public AICell MovedCell; // the mover's cell data before this move (pre-promotion)
    public int ToRow;
    public int ToCol;
    public List<AIPosition> CapturedPositions;
    public List<AICell> CapturedCells;
}

// Pure-data port of MoveGenerator's rules (quiet moves, captures incl. flying kings/men-capture-
// backward, mandatory-maximum-capture, promotion, the "is this square immediately capturable"
// safety check) operating on a plain AICell[,] instead of live Piece/Block objects. No
// ServiceLocator, no MonoBehaviour, no Unity API calls anywhere here - safe to call from any
// thread, which is the whole reason it exists (see BotMinimax).
public static class BoardState
{
    private static readonly (int dRow, int dCol)[] DiagonalDirections =
    {
        (1, -1), (1, 1), (-1, -1), (-1, 1)
    };

    private static readonly (int dRow, int dCol)[] OrthogonalDirections =
    {
        (1, 0), (-1, 0), (0, -1), (0, 1)
    };

    private static (int dRow, int dCol)[] Directions(AIRules rules)
    {
        return rules.MovementScheme == MovementScheme.Orthogonal ? OrthogonalDirections : DiagonalDirections;
    }

    private static int ForwardDirection(int playerID)
    {
        return playerID == 2 ? 1 : -1;
    }

    private static bool IsValidPosition(int row, int col, AIRules rules)
    {
        return row >= 0 && row < rules.Rows && col >= 0 && col < rules.Columns;
    }

    public static bool IsPromotionRow(int row, int playerID, AIRules rules)
    {
        return playerID == 2 ? row == rules.Rows - 1 : row == 0;
    }

    // Non-king pieces may move in every direction except straight backward - see
    // MoveGenerator.GetMoveDirections for why that's the right generalization across both schemes.
    private static IEnumerable<(int dRow, int dCol)> GetMoveDirections(bool isKing, int playerID, AIRules rules)
    {
        int forward = ForwardDirection(playerID);
        foreach ((int dRow, int dCol) dir in Directions(rules))
        {
            if (isKing || dir.dRow != -forward)
            {
                yield return dir;
            }
        }
    }

    private static IEnumerable<(int dRow, int dCol)> GetCaptureDirections(bool isKing, int playerID, AIRules rules)
    {
        if (isKing || rules.MenCaptureBackward)
        {
            return Directions(rules);
        }
        return GetMoveDirections(isKing, playerID, rules);
    }

    private static bool ContainsPosition(List<AIPosition> positions, AIPosition target)
    {
        for (int i = 0; i < positions.Count; i++)
        {
            if (positions[i].Row == target.Row && positions[i].Col == target.Col) { return true; }
        }
        return false;
    }

    // A square is passable for a flying king's outward search / landing enumeration if it's
    // genuinely empty - mirrors MoveGenerator.IsPassable, see there for why DeferCaptureRemoval
    // additionally excludes squares captured earlier in this same chain.
    private static bool IsPassable(AICell[,] cells, int row, int col, List<AIPosition> capturedSoFar, AIRules rules)
    {
        if (!IsValidPosition(row, col, rules) || cells[row, col].PlayerID != 0) { return false; }
        return !rules.DeferCaptureRemoval || !ContainsPosition(capturedSoFar, new AIPosition(row, col));
    }

    public static List<AIPosition> GetQuietMoves(AICell[,] cells, int row, int col, AIRules rules)
    {
        List<AIPosition> positions = new();
        AICell cell = cells[row, col];
        bool flying = cell.IsKing && rules.FlyingKings;

        foreach ((int dRow, int dCol) dir in GetMoveDirections(cell.IsKing, cell.PlayerID, rules))
        {
            int targetRow = row + dir.dRow;
            int targetCol = col + dir.dCol;

            while (IsValidPosition(targetRow, targetCol, rules) && cells[targetRow, targetCol].PlayerID == 0)
            {
                positions.Add(new AIPosition(targetRow, targetCol));

                if (!flying) { break; }

                targetRow += dir.dRow;
                targetCol += dir.dCol;
            }
        }
        return positions;
    }

    // Every maximal capture chain reachable from (row,col), of any length - mirrors
    // MoveGenerator.FindCaptureSequences.
    public static List<AICaptureSequence> FindCaptureSequences(AICell[,] cells, int row, int col, AIRules rules)
    {
        List<AICaptureSequence> results = new();
        SearchCaptures(cells, row, col, new List<AIPosition>(), new List<bool>(), new List<AIPosition>(), null, rules, results);
        return results;
    }

    private static void SearchCaptures(AICell[,] cells, int fromRow, int fromCol, List<AIPosition> capturedSoFar, List<bool> capturedWasKing, List<AIPosition> landingsSoFar, (int dRow, int dCol)? lastDirection, AIRules rules, List<AICaptureSequence> results)
    {
        AICell mover = cells[fromRow, fromCol];
        bool foundFurtherCapture = false;
        bool flying = mover.IsKing && rules.FlyingKings;

        foreach ((int dRow, int dCol) dir in GetCaptureDirections(mover.IsKing, mover.PlayerID, rules))
        {
            // The "no 180-degree turn" rule (Turkish): mirrors MoveGenerator.SearchCaptures.
            if (rules.ForbidImmediateReversal && lastDirection.HasValue
                && dir.dRow == -lastDirection.Value.dRow && dir.dCol == -lastDirection.Value.dCol)
            {
                continue;
            }

            int middleRow = fromRow + dir.dRow;
            int middleCol = fromCol + dir.dCol;

            if (flying)
            {
                while (IsPassable(cells, middleRow, middleCol, capturedSoFar, rules))
                {
                    middleRow += dir.dRow;
                    middleCol += dir.dCol;
                }
            }

            if (!IsValidPosition(middleRow, middleCol, rules) || cells[middleRow, middleCol].PlayerID == 0) { continue; }

            AICell middleCell = cells[middleRow, middleCol];
            if (middleCell.PlayerID == mover.PlayerID) { continue; }

            // Immunity (Italian): a man can never capture a King - only an opposing King may.
            if (rules.MenCannotCaptureKings && !mover.IsKing && middleCell.IsKing) { continue; }

            AIPosition middlePos = new AIPosition(middleRow, middleCol);
            if (ContainsPosition(capturedSoFar, middlePos)) { continue; } // already captured earlier in this chain

            int landingRow = middleRow + dir.dRow;
            int landingCol = middleCol + dir.dCol;

            while (IsPassable(cells, landingRow, landingCol, capturedSoFar, rules))
            {
                foundFurtherCapture = true;
                AIPosition landingPos = new AIPosition(landingRow, landingCol);

                cells[middleRow, middleCol] = default;
                cells[fromRow, fromCol] = default;
                cells[landingRow, landingCol] = mover;

                capturedSoFar.Add(middlePos);
                capturedWasKing.Add(middleCell.IsKing);
                landingsSoFar.Add(landingPos);

                // Continue the chain from the new landing square, not the original square.
                SearchCaptures(cells, landingRow, landingCol, capturedSoFar, capturedWasKing, landingsSoFar, dir, rules, results);

                capturedSoFar.RemoveAt(capturedSoFar.Count - 1);
                capturedWasKing.RemoveAt(capturedWasKing.Count - 1);
                landingsSoFar.RemoveAt(landingsSoFar.Count - 1);

                cells[landingRow, landingCol] = default;
                cells[fromRow, fromCol] = mover;
                cells[middleRow, middleCol] = middleCell;

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

            results.Add(new AICaptureSequence
            {
                Landings = new List<AIPosition>(landingsSoFar),
                Captured = new List<AIPosition>(capturedSoFar),
                CapturedKingCount = kingCount,
                FirstKingCaptureIndex = firstKingIndex
            });
        }
    }

    // One board square paired with one of its own candidate capture sequences - mirrors
    // MoveGenerator.CaptureCandidate, see there for why some tiers need this instead of just a
    // per-square sequence list.
    private struct CaptureCandidate
    {
        public int FromRow;
        public int FromCol;
        public bool IsKingMover;
        public AICaptureSequence Sequence;
    }

    private static List<CaptureCandidate> GetCaptureCandidates(AICell[,] cells, int playerID, AIRules rules)
    {
        List<CaptureCandidate> candidates = new();
        for (int r = 0; r < rules.Rows; r++)
        {
            for (int c = 0; c < rules.Columns; c++)
            {
                if (cells[r, c].PlayerID != playerID) { continue; }

                List<AICaptureSequence> sequences = FindCaptureSequences(cells, r, c, rules);
                for (int i = 0; i < sequences.Count; i++)
                {
                    candidates.Add(new CaptureCandidate
                    {
                        FromRow = r,
                        FromCol = c,
                        IsKingMover = cells[r, c].IsKing,
                        Sequence = sequences[i]
                    });
                }
            }
        }
        return candidates;
    }

    // Mirrors MoveGenerator.ApplyMandatoryCaptureTiers - see there for the tier order/rationale.
    private static List<CaptureCandidate> ApplyMandatoryCaptureTiers(List<CaptureCandidate> candidates, AIRules rules)
    {
        if (candidates.Count == 0 || !rules.MustCaptureMaximum) { return candidates; }

        int maxLength = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Sequence.Length > maxLength) { maxLength = candidates[i].Sequence.Length; }
        }
        candidates = candidates.FindAll(c => c.Sequence.Length == maxLength);

        if (rules.PreferKingMover && candidates.Exists(c => c.IsKingMover))
        {
            candidates = candidates.FindAll(c => c.IsKingMover);
        }

        if (rules.PreferKingCaptures)
        {
            int maxKingCount = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Sequence.CapturedKingCount > maxKingCount) { maxKingCount = candidates[i].Sequence.CapturedKingCount; }
            }
            candidates = candidates.FindAll(c => c.Sequence.CapturedKingCount == maxKingCount);
        }

        if (rules.PreferEarlierKingCapture)
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

    // Every legal move for playerID in the board's current state - mirrors
    // MoveGenerator.CheckMovablePieces plus PopulateMoveData, just against AICell[,] instead of
    // Piece/Block. Capturing is always mandatory: if any capture is available anywhere on the
    // board, only pieces with a qualifying capture (per ApplyMandatoryCaptureTiers) are selectable
    // at all, quiet moves aren't legal for anyone that turn; otherwise any capture qualifies.
    public static List<AIMoveOption> GetLegalMoves(AICell[,] cells, int playerID, AIRules rules)
    {
        List<AIMoveOption> moves = new();
        List<CaptureCandidate> allCandidates = GetCaptureCandidates(cells, playerID, rules);

        if (allCandidates.Count > 0)
        {
            List<CaptureCandidate> qualifying = ApplyMandatoryCaptureTiers(allCandidates, rules);
            for (int i = 0; i < qualifying.Count; i++)
            {
                moves.Add(new AIMoveOption { FromRow = qualifying[i].FromRow, FromCol = qualifying[i].FromCol, Sequence = qualifying[i].Sequence });
            }
            return moves;
        }

        for (int r = 0; r < rules.Rows; r++)
        {
            for (int c = 0; c < rules.Columns; c++)
            {
                if (cells[r, c].PlayerID != playerID) { continue; }

                List<AIPosition> quiet = GetQuietMoves(cells, r, c, rules);
                for (int i = 0; i < quiet.Count; i++)
                {
                    moves.Add(new AIMoveOption { FromRow = r, FromCol = c, Position = quiet[i] });
                }
            }
        }
        return moves;
    }

    // Applies a move directly on the given snapshot (mutate in place, paired with UndoMove) -
    // mirrors BotMinimax's old live-board apply/undo, just against AICell[,].
    public static AIUndoInfo ApplyMove(AICell[,] cells, AIMoveOption move, AIRules rules)
    {
        AICell mover = cells[move.FromRow, move.FromCol];

        AIUndoInfo undo = new()
        {
            FromRow = move.FromRow,
            FromCol = move.FromCol,
            MovedCell = mover,
            CapturedPositions = new List<AIPosition>(),
            CapturedCells = new List<AICell>()
        };

        if (move.Sequence != null)
        {
            for (int i = 0; i < move.Sequence.Captured.Count; i++)
            {
                AIPosition capturedAt = move.Sequence.Captured[i];
                undo.CapturedPositions.Add(capturedAt);
                undo.CapturedCells.Add(cells[capturedAt.Row, capturedAt.Col]);
                cells[capturedAt.Row, capturedAt.Col] = default;
            }
        }

        cells[move.FromRow, move.FromCol] = default;

        AIPosition landing = move.Sequence != null
            ? move.Sequence.Landings[move.Sequence.Landings.Count - 1]
            : move.Position;

        undo.ToRow = landing.Row;
        undo.ToCol = landing.Col;

        AICell placed = mover;
        if (!mover.IsKing && IsPromotionRow(landing.Row, mover.PlayerID, rules))
        {
            placed.IsKing = true;
        }
        cells[landing.Row, landing.Col] = placed;

        return undo;
    }

    public static void UndoMove(AICell[,] cells, AIUndoInfo undo)
    {
        cells[undo.ToRow, undo.ToCol] = default;
        cells[undo.FromRow, undo.FromCol] = undo.MovedCell;

        for (int i = 0; i < undo.CapturedPositions.Count; i++)
        {
            AIPosition pos = undo.CapturedPositions[i];
            cells[pos.Row, pos.Col] = undo.CapturedCells[i];
        }
    }

    // Whether the piece at (row,col) could be immediately captured by an adjacent enemy - mirrors
    // MoveGenerator.IsSafe (a move-ordering/evaluation heuristic, not a rule-legality check, so it
    // only ever checks an immediate adjacent threat, never a flying king bearing down from afar).
    public static bool IsSafe(AICell[,] cells, int row, int col, AIRules rules)
    {
        AICell piece = cells[row, col];

        // Only valid for diagonal movement - see MoveGenerator.IsSafe's matching comment for why an
        // orthogonal piece (Turkish) on an edge is still fully exposed along the other axis.
        if (rules.MovementScheme == MovementScheme.Diagonal
            && (col == 0 || col == rules.Columns - 1 || row == 0 || row == rules.Rows - 1))
        {
            return true;
        }

        int forward = ForwardDirection(piece.PlayerID);

        foreach ((int dRow, int dCol) dir in Directions(rules))
        {
            int enemyRow = row + dir.dRow;
            int enemyCol = col + dir.dCol;

            if (!IsValidPosition(enemyRow, enemyCol, rules) || cells[enemyRow, enemyCol].PlayerID == 0)
            {
                continue;
            }

            AICell enemy = cells[enemyRow, enemyCol];
            if (enemy.PlayerID == piece.PlayerID)
            {
                continue;
            }

            // Immunity (Italian): a man threatens nothing if piece is a King - only an opposing
            // King could actually capture it.
            if (rules.MenCannotCaptureKings && piece.IsKing && !enemy.IsKing)
            {
                continue;
            }

            // See MoveGenerator.IsSafe's matching comment - MenCaptureBackward rulesets let a mere
            // man capture backward too, so this must agree with GetCaptureDirections's own gate.
            bool enemyMovingBackward = dir.dRow != forward;
            if (enemyMovingBackward && !rules.MenCaptureBackward && !enemy.IsKing)
            {
                continue;
            }

            int landingRow = row - dir.dRow;
            int landingCol = col - dir.dCol;

            if (IsValidPosition(landingRow, landingCol, rules) && cells[landingRow, landingCol].PlayerID == 0)
            {
                return false;
            }
        }

        return true;
    }
}
