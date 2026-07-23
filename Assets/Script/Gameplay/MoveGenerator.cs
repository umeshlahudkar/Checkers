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

    private Block[,] Board => ServiceLocator.Get<GameplayController>().board;

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
            if (IsValidPosition(adjRow, adjCol) && !Board[adjRow, adjCol].IsPiecePresent)
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
            while (IsValidPosition(targetRow, targetCol) && !Board[targetRow, targetCol].IsPiecePresent)
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
                while (IsValidPosition(middleRow, middleCol) && !Board[middleRow, middleCol].IsPiecePresent)
                {
                    middleRow += dir.dRow;
                    middleCol += dir.dCol;
                }
            }

            if (!IsValidPosition(middleRow, middleCol) || !Board[middleRow, middleCol].IsPiecePresent) { continue; }

            Piece middlePiece = Board[middleRow, middleCol].Piece;
            if (middlePiece.Player_ID == piece.Player_ID) { continue; }

            BoardPosition middlePos = new BoardPosition(middleRow, middleCol);
            if (capturedSoFar.Contains(middlePos)) { continue; } // already captured earlier in this chain

            // Enumerate landing squares beyond the captured piece: just the one immediately behind
            // it for a fixed-distance capture, or every empty square up to the next
            // obstruction/edge for a flying king.
            int landingRow = middleRow + dir.dRow;
            int landingCol = middleCol + dir.dCol;

            while (IsValidPosition(landingRow, landingCol) && !Board[landingRow, landingCol].IsPiecePresent)
            {
                foundFurtherCapture = true;
                BoardPosition landingPos = new BoardPosition(landingRow, landingCol);

                // Simulate the jump (reusing the same mutate-then-revert technique the old
                // single/double-kill checks used), then recurse to look for further jumps from the
                // new landing before reverting.
                Board[middleRow, middleCol].SetBlockPiece(false, null);
                Board[fromRow, fromCol].SetBlockPiece(false, null);
                Board[landingRow, landingCol].SetBlockPiece(true, piece);

                capturedSoFar.Add(middlePos);
                landingsSoFar.Add(landingPos);

                SearchCaptures(piece, capturedSoFar, landingsSoFar, results);

                capturedSoFar.RemoveAt(capturedSoFar.Count - 1);
                landingsSoFar.RemoveAt(landingsSoFar.Count - 1);

                Board[landingRow, landingCol].SetBlockPiece(false, null);
                Board[fromRow, fromCol].SetBlockPiece(true, piece);
                Board[middleRow, middleCol].SetBlockPiece(true, middlePiece);

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

    public bool IsSafeToMove(Piece piece, int targetRow, int targetCol)
    {
        int initialPieceRow = piece.Row_ID;
        int initialPieceCol = piece.Coloum_ID;

        Board[initialPieceRow, initialPieceCol].SetBlockPiece(false, null);
        Board[targetRow, targetCol].SetBlockPiece(true, piece);

        bool isSafe = IsSafe(piece);

        Board[targetRow, targetCol].SetBlockPiece(false, null);
        Board[initialPieceRow, initialPieceCol].SetBlockPiece(true, piece);

        return isSafe;
    }

    // Whether the piece ends up safe after playing out the *entire* given sequence (captured
    // pieces removed, piece moved to the final landing) - generalizes the old single/double-hop
    // safety checks to a sequence of any length.
    public bool IsSequenceSafe(Piece piece, CaptureSequence sequence)
    {
        int originalRow = piece.Row_ID;
        int originalCol = piece.Coloum_ID;

        List<Piece> removedPieces = new();
        for (int i = 0; i < sequence.Captured.Count; i++)
        {
            BoardPosition captured = sequence.Captured[i];
            removedPieces.Add(Board[captured.row_ID, captured.col_ID].Piece);
            Board[captured.row_ID, captured.col_ID].SetBlockPiece(false, null);
        }

        Board[originalRow, originalCol].SetBlockPiece(false, null);

        BoardPosition finalLanding = sequence.Landings[sequence.Landings.Count - 1];
        Board[finalLanding.row_ID, finalLanding.col_ID].SetBlockPiece(true, piece);

        bool isSafe = IsSafe(piece);

        Board[finalLanding.row_ID, finalLanding.col_ID].SetBlockPiece(false, null);
        Board[originalRow, originalCol].SetBlockPiece(true, piece);

        for (int i = 0; i < sequence.Captured.Count; i++)
        {
            BoardPosition captured = sequence.Captured[i];
            Board[captured.row_ID, captured.col_ID].SetBlockPiece(true, removedPieces[i]);
        }

        return isSafe;
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

            if (!IsValidPosition(enemyRow, enemyCol) || !Board[enemyRow, enemyCol].IsPiecePresent)
            {
                continue;
            }

            Piece enemy = Board[enemyRow, enemyCol].Piece;
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

            if (IsValidPosition(landingRow, landingCol) && !Board[landingRow, landingCol].IsPiecePresent)
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

    // Populates movablePositions/captureSequences for every piece in the list - a prerequisite for
    // TryGetBestCapture/TryGetBestMove, which rank pre-computed options rather than deriving them.
    public void PopulateMoveData(List<Piece> movablePieces)
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece piece = movablePieces[i];
            piece.ResetAllList();
            SetPiecePosition(piece);
        }
    }

    // Finds the longest capture available across all movable pieces, then (unless preferSafe is
    // null) prefers one that leaves the piece safe afterward, falling back to any at that same
    // longest length if none are safe. Shared by BotPlayer (Hard/Medium difficulty) and the Hint
    // feature, which always wants this same "objectively best" ranking regardless of bot difficulty.
    public bool TryGetBestCapture(List<Piece> movablePieces, bool? preferSafe, out Piece piece, out CaptureSequence sequence)
    {
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

        if (maxLength == 0)
        {
            piece = null;
            sequence = null;
            return false;
        }

        (Piece piece, CaptureSequence sequence)? fallback = null;

        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece candidatePiece = movablePieces[i];
            List<CaptureSequence> sequences = candidatePiece.captureSequences;
            for (int j = 0; j < sequences.Count; j++)
            {
                CaptureSequence candidateSequence = sequences[j];
                if (candidateSequence.Length != maxLength) { continue; }

                if (!preferSafe.HasValue || IsSequenceSafe(candidatePiece, candidateSequence) == preferSafe.Value)
                {
                    piece = candidatePiece;
                    sequence = candidateSequence;
                    return true;
                }

                fallback ??= (candidatePiece, candidateSequence);
            }
        }

        if (fallback.HasValue)
        {
            piece = fallback.Value.piece;
            sequence = fallback.Value.sequence;
            return true;
        }

        piece = null;
        sequence = null;
        return false;
    }

    public bool TryGetBestMove(List<Piece> movablePieces, bool? preferSafe, out Piece piece, out BoardPosition position)
    {
        for (int i = 0; i < movablePieces.Count; i++)
        {
            Piece candidatePiece = movablePieces[i];
            for (int j = 0; j < candidatePiece.movablePositions.Count; j++)
            {
                BoardPosition candidatePosition = candidatePiece.movablePositions[j];

                if (preferSafe.HasValue && IsSafeToMove(candidatePiece, candidatePosition.row_ID, candidatePosition.col_ID) != preferSafe.Value)
                {
                    continue;
                }

                piece = candidatePiece;
                position = candidatePosition;
                return true;
            }
        }

        piece = null;
        position = default;
        return false;
    }
}
