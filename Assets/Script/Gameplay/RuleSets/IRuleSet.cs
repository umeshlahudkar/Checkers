public interface IRuleSet
{
    int Rows { get; }
    int Columns { get; }
    int PieceRowsPerSide { get; }

    // Kings slide any distance along an empty diagonal and can capture from a distance, instead of
    // moving/capturing exactly one/two squares.
    bool FlyingKings { get; }

    // Whether non-king pieces may capture in their backward direction (International) or only
    // forward (American).
    bool MenCaptureBackward { get; }

    // Whether a player must play the capture sequence that takes the most pieces, rather than any
    // legal capture.
    bool MustCaptureMaximum { get; }

    bool IsPromotionRow(int row, int playerID);

    // Number of consecutive turns (across both players) without a capture or promotion before the
    // match is called a draw - prevents a shuffle that makes no progress from running forever. A
    // simplified, single-threshold stand-in for the real tournament rule (which shortens the limit
    // for certain reduced endgame material) - that nuance is out of scope here.
    int NoProgressMoveLimit { get; }
}
