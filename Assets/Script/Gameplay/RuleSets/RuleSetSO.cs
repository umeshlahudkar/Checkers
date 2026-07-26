using UnityEngine;

[CreateAssetMenu(fileName = "RuleSet", menuName = "Scriptable/RuleSet")]
public class RuleSetSO : ScriptableObject, IRuleSet
{
    [SerializeField] private string displayName;
    [TextArea] [SerializeField] private string description;

    [SerializeField] private int rows = 8;
    [SerializeField] private int columns = 8;
    [SerializeField] private int pieceRowsPerSide = 3;

    [SerializeField] private bool flyingKings;
    [SerializeField] private bool menCaptureBackward;
    [SerializeField] private bool mustCaptureMaximum;

    [SerializeField] private int noProgressMoveLimit = 80;

    public string DisplayName => displayName;
    public string Description => description;

    public int Rows => rows;
    public int Columns => columns;
    public int PieceRowsPerSide => pieceRowsPerSide;

    public bool FlyingKings => flyingKings;
    public bool MenCaptureBackward => menCaptureBackward;
    public bool MustCaptureMaximum => mustCaptureMaximum;

    public int NoProgressMoveLimit => noProgressMoveLimit;

    public bool IsPromotionRow(int row, int playerID)
    {
        return playerID == 2 ? row == rows - 1 : row == 0;
    }
}
