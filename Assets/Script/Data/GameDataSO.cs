using UnityEngine;


[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable/GameData")]
public class GameDataSO : ScriptableObject
{
    public GameModeType gameMode;
    public PlayerInfo ownPlayer;
    public PlayerInfo opponentPlayer;
    public BotDifficulty botDifficulty = BotDifficulty.Easy;
    public RuleSetSO ruleSet;
    public bool enableTurnTimer = true;
    public BotAISettingsSO botAISettings;

    // True whenever the local match's second player is actually AI-controlled, independent of
    // gameMode - gameMode may report Multiplayer even for a bot opponent (see OnlineModeHandler's
    // matchmaking-timeout fallback, which disguises a bot as a real opponent by deliberately NOT
    // changing gameMode), so GameManager can't use gameMode alone to decide which player prefab to
    // spawn in a locally-simulated match. Set explicitly by whichever handler sets up the match
    // (PvcModeHandler, or OnlineModeHandler's bot fallback) - never inferred from gameMode.
    public bool opponentIsBot;
}
