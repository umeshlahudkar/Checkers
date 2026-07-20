using UnityEngine;


[CreateAssetMenu(fileName = "GameData", menuName = "Scriptable/GameData")]
public class GameDataSO : ScriptableObject
{
    public GameModeType gameMode;
    public PlayerInfo ownPlayer;
    public PlayerInfo opponentPlayer;
    public BotDifficulty botDifficulty = BotDifficulty.Easy;
    public RuleSetSO ruleSet;
}
