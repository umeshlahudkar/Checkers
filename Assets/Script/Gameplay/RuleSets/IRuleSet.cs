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
}
