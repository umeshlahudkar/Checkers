[System.Serializable]
public struct GameSettingsData
{
    public const string FileName = "GameSettingsData.json";

    public int boardThemeIndex;
    public bool showMoveHints;
    public bool vibrationEnabled;
}
