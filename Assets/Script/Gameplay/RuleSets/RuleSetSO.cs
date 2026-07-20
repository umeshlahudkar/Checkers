using UnityEngine;

[CreateAssetMenu(fileName = "RuleSet", menuName = "Scriptable/RuleSet")]
public class RuleSetSO : ScriptableObject, IRuleSet
{
    [SerializeField] private int rows = 8;
    [SerializeField] private int columns = 8;
    [SerializeField] private int pieceRowsPerSide = 3;

    [SerializeField] private bool flyingKings;
    [SerializeField] private bool menCaptureBackward;
    [SerializeField] private bool mustCaptureMaximum;

    public int Rows => rows;
    public int Columns => columns;
    public int PieceRowsPerSide => pieceRowsPerSide;

    public bool FlyingKings => flyingKings;
    public bool MenCaptureBackward => menCaptureBackward;
    public bool MustCaptureMaximum => mustCaptureMaximum;

    public bool IsPromotionRow(int row, int playerID)
    {
        return playerID == 2 ? row == rows - 1 : row == 0;
    }
}
