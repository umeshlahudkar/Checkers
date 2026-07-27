public interface IRuleSet
{
    int Rows { get; }
    int Columns { get; }
    int PieceRowsPerSide { get; }

    // Diagonal (draughts/checkers) or Orthogonal (Turkish dama) movement/capture geometry.
    MovementScheme MovementScheme { get; }

    // Whether every square of the starting rows gets a piece (Turkish dama) rather than only the
    // alternating dark squares (draughts/checkers).
    bool PiecesOnAllSquares { get; }

    // Whether each player's very back row is left empty at setup, with PieceRowsPerSide filled
    // starting one row further in instead (Turkish dama) - most variants fill starting from the
    // back row itself.
    bool LeaveBackRowEmpty { get; }

    // Kings slide any distance along an empty line (per MovementScheme) and can capture from a
    // distance, instead of moving/capturing exactly one/two squares.
    bool FlyingKings { get; }

    // Whether non-king pieces may capture in their backward direction (International) or only
    // forward (American).
    bool MenCaptureBackward { get; }

    // Whether a player must play the capture sequence that takes the most pieces, rather than any
    // legal capture.
    bool MustCaptureMaximum { get; }

    // Whether a man may not capture an enemy King at all - only an opposing King may capture a King
    // (Italian). Doesn't affect King-vs-King or King-vs-man captures.
    bool MenCannotCaptureKings { get; }

    // Under MustCaptureMaximum, cascading tiebreaks applied in this order when multiple sequences
    // are tied on the previous tier - each is independently optional per ruleset:
    // 1) Length (always applied - the base "maximum capture" rule itself).
    // 2) PreferKingMover: a King's sequence beats a man's sequence (Italian "King Dominance").
    // 3) PreferKingCaptures: whichever captures the most Kings (Brazilian/Italian "Target Kings").
    // 4) PreferEarlierKingCapture: whichever captures its first King soonest in the sequence
    //    (Italian "First Blood").
    bool PreferKingMover { get; }
    bool PreferKingCaptures { get; }
    bool PreferEarlierKingCapture { get; }

    // What happens when a piece reaches the promotion row mid-capture-sequence rather than as its
    // turn's final landing square - see MidChainPromotionRule for the three behaviors.
    MidChainPromotionRule MidChainPromotionRule { get; }

    // Whether a captured piece stays on the board (blocking that square, but not recapturable)
    // until the whole turn is over (International), rather than being removed the instant it's
    // jumped, which can open up new paths mid-turn (American/Russian).
    bool DeferCaptureRemoval { get; }

    // Whether a piece may not immediately reverse direction from one hop to the next within the
    // same capture sequence - i.e. hop N+1 can't be the exact opposite direction of hop N, since
    // that would fly back through the square the piece just left (Turkish "no 180-degree turn").
    bool ForbidImmediateReversal { get; }

    bool IsPromotionRow(int row, int playerID);

    // Which color takes the very first turn of the match (Italian/Spanish/Canadian: White; Pool
    // Checkers: Black) - PieceType.None for every ruleset that doesn't fix an opening color, which
    // keeps the existing "player 1 always opens" behavior for them (see
    // GameManager.DetermineFirstTurnPlayer).
    PieceType FirstMoveColor { get; }

    // Whether the bottom-right corner square is dark (Italian's "cantone" convention) rather than
    // the light/white corner every other modeled ruleset uses - purely cosmetic, doesn't affect
    // legality (see BoardGenerator.GenerateBoard).
    bool DarkSquareBottomRight { get; }

    // Number of consecutive turns (across both players) without a capture or promotion before the
    // match is called a draw - prevents a shuffle that makes no progress from running forever. A
    // simplified, single-threshold stand-in for the real tournament rule (which shortens the limit
    // for certain reduced endgame material) - that nuance is out of scope here.
    int NoProgressMoveLimit { get; }

    // Whether a player instantly loses if reduced to exactly one non-king piece while the opponent
    // still has at least one King (Turkish dama's single-man-vs-Dama rule).
    bool SingleManLosesToKing { get; }
}
