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
}
