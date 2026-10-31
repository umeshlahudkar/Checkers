[System.Serializable]
public struct GameSettingsData
{
    public const string FileName = "GameSettingsData.json";

    public bool showMoveHints;
    public bool vibrationEnabled;
    public int ruleSetIndex;
    public BotDifficulty botDifficulty;
}
