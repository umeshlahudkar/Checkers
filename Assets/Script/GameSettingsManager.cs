using System.Collections;
using UnityEngine;

public class GameSettingsManager : Service<GameSettingsManager>, IInitializable
{
    [SerializeField] private BoardThemeListSO boardThemeList;

    private int boardThemeIndex;
    private bool showMoveHints;
    private bool vibrationEnabled;

    public int BoardThemeCount { get { return boardThemeList.themes.Count; } }
    public bool ShowMoveHints { get { return showMoveHints; } }
    public bool VibrationEnabled { get { return vibrationEnabled; } }

    public IEnumerator Initialize()
    {
        boardThemeIndex = 0;
        showMoveHints = true;
        vibrationEnabled = true;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        if (SavingSystem.Exists(GameSettingsData.FileName))
        {
            GameSettingsData data = SavingSystem.Load<GameSettingsData>(GameSettingsData.FileName);

            boardThemeIndex = data.boardThemeIndex;
            showMoveHints = data.showMoveHints;
            vibrationEnabled = data.vibrationEnabled;
        }
#endif

        yield break;
    }

    public BoardThemeInfo GetBoardTheme(int index)
    {
        return boardThemeList.themes[index];
    }

    public int GetBoardThemeIndex()
    {
        return boardThemeIndex;
    }

    public void SetBoardTheme(int index)
    {
        boardThemeIndex = index;
        Save();
    }

    public void SetShowMoveHints(bool value)
    {
        showMoveHints = value;
        Save();
    }

    public void SetVibrationEnabled(bool value)
    {
        vibrationEnabled = value;
        Save();
    }

    private void Save()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        GameSettingsData data = new()
        {
            boardThemeIndex = boardThemeIndex,
            showMoveHints = showMoveHints,
            vibrationEnabled = vibrationEnabled
        };

        SavingSystem.Save(GameSettingsData.FileName, data);
#endif
    }
}
