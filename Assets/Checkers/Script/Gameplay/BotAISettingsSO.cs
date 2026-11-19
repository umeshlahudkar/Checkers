using UnityEngine;

// Tuning knobs for BotMinimax's search depth (per difficulty) and evaluation weights - pulled out
// to a designer-editable asset instead of hardcoded constants, same pattern as RuleSetSO.
[CreateAssetMenu(fileName = "BotAISettings", menuName = "Scriptable/BotAISettings")]
public class BotAISettingsSO : ScriptableObject
{
    [Header("Search Depth")]
    public int easyDepth = 1;
    public int mediumDepth = 2;
    public int hardDepth = 4;

    [Header("Material")]
    public int manScore = 100;
    public int kingScore = 180;

    [Header("Promotion")]
    public int nearPromotionDistance = 2;
    public int nearPromotionBonus = 30;

    [Header("Safety")]
    public int protectedBonus = 15;
    public int vulnerablePenalty = 40;

    [Header("Position")]
    public int centerMargin = 2;
    public int centerBonus = 10;
    public int mobilityWeight = 2;

    [Header("Stalemate Sacrifice")]
    // Turns without a capture or a promotion before the bot deliberately concedes about one man
    // to get the game moving again.
    //
    // Nothing in BotMinimax's evaluation distinguishes "lost" from "behind" - it just maximises
    // the same score difference, and the safety and mobility terms happen to add up to a
    // competent drawing strategy. So two sides that cannot make progress shuffle until the
    // position repeats or the no-progress counter runs out, and a game the player was winning
    // ends in a draw. This breaks that deadlock.
    //
    // Per difficulty, because the patience that reads as "a tough opponent" on Hard reads as
    // "this game is stuck" on Easy. 0 disables it for that difficulty.
    public int easyStallSacrificeMoveLimit = 4;
    public int mediumStallSacrificeMoveLimit = 6;
    public int hardStallSacrificeMoveLimit = 10;

    public int StallSacrificeMoveLimit(BotDifficulty difficulty)
    {
        return difficulty switch
        {
            BotDifficulty.Hard => hardStallSacrificeMoveLimit,
            BotDifficulty.Medium => mediumStallSacrificeMoveLimit,
            _ => easyStallSacrificeMoveLimit,
        };
    }
}
