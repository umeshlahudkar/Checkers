using UnityEngine;

[CreateAssetMenu(fileName = "RuleSet", menuName = "Scriptable/RuleSet")]
public class RuleSetSO : ScriptableObject, IRuleSet
{
    [SerializeField] private string displayName;

    // Short, single-line blurb shown on the mode-selection card itself.
    [TextArea] [SerializeField] private string description;

    // Full "how this variant plays" explanation, shown in a separate rules popup - a single block
    // of TMP rich text with <b>Header</b> lines and blank-line spacing between sections, meant to
    // be dropped straight into a TextMeshProUGUI with rich text enabled.
    [TextArea(5, 20)] [SerializeField] private string longDescription;

    [SerializeField] private int rows = 8;
    [SerializeField] private int columns = 8;
    [SerializeField] private int pieceRowsPerSide = 3;

    [SerializeField] private MovementScheme movementScheme = MovementScheme.Diagonal;
    [SerializeField] private bool piecesOnAllSquares;
    [SerializeField] private bool leaveBackRowEmpty;

    [SerializeField] private bool flyingKings;
    [SerializeField] private bool menCaptureBackward;
    [SerializeField] private bool menCannotCaptureKings;
    [SerializeField] private bool mustCaptureMaximum;
    [SerializeField] private bool preferKingMover;
    [SerializeField] private bool preferKingCaptures;
    [SerializeField] private bool preferEarlierKingCapture;

    [SerializeField] private MidChainPromotionRule midChainPromotionRule = MidChainPromotionRule.EndsTurnOnPromotion;
    [SerializeField] private bool deferCaptureRemoval;
    [SerializeField] private bool forbidImmediateReversal;

    [SerializeField] private int noProgressMoveLimit = 80;
    [SerializeField] private bool singleManLosesToKing;

    public string DisplayName => displayName;
    public string Description => description;
    public string LongDescription => longDescription;

    public int Rows => rows;
    public int Columns => columns;
    public int PieceRowsPerSide => pieceRowsPerSide;

    public MovementScheme MovementScheme => movementScheme;
    public bool PiecesOnAllSquares => piecesOnAllSquares;
    public bool LeaveBackRowEmpty => leaveBackRowEmpty;

    public bool FlyingKings => flyingKings;
    public bool MenCaptureBackward => menCaptureBackward;
    public bool MenCannotCaptureKings => menCannotCaptureKings;
    public bool MustCaptureMaximum => mustCaptureMaximum;
    public bool PreferKingMover => preferKingMover;
    public bool PreferKingCaptures => preferKingCaptures;
    public bool PreferEarlierKingCapture => preferEarlierKingCapture;

    public MidChainPromotionRule MidChainPromotionRule => midChainPromotionRule;
    public bool DeferCaptureRemoval => deferCaptureRemoval;
    public bool ForbidImmediateReversal => forbidImmediateReversal;

    public int NoProgressMoveLimit => noProgressMoveLimit;
    public bool SingleManLosesToKing => singleManLosesToKing;

    public bool IsPromotionRow(int row, int playerID)
    {
        return playerID == 2 ? row == rows - 1 : row == 0;
    }
}
