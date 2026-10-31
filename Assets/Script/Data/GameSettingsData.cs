[System.Serializable]
public struct GameSettingsData
{
    public const string FileName = "GameSettingsData.json";

    public bool showMoveHints;
    public bool vibrationEnabled;
    public int ruleSetIndex;
    public BotDifficulty botDifficulty;

    // Which mode the player last actually started a match in - the main menu highlights that tile
    // on open so the common case is one tap away without adding another control to the screen.
    public GameModeType lastMode;
}
