using System.Collections;
using UnityEngine;

public class GameSettingsManager : Service<GameSettingsManager>, IInitializable
{
    [SerializeField] private RuleSetListSO ruleSetList;

    private bool showMoveHints;
    private bool vibrationEnabled;
    private int ruleSetIndex;
    private BotDifficulty botDifficulty;

    public bool ShowMoveHints { get { return showMoveHints; } }
    public bool VibrationEnabled { get { return vibrationEnabled; } }
    public int RuleSetCount { get { return ruleSetList.ruleSets.Count; } }

    public IEnumerator Initialize()
    {
        showMoveHints = true;
        vibrationEnabled = true;
        ruleSetIndex = 0;
        botDifficulty = BotDifficulty.Easy;

#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        if (SavingSystem.Exists(GameSettingsData.FileName))
        {
            GameSettingsData data = SavingSystem.Load<GameSettingsData>(GameSettingsData.FileName);

            showMoveHints = data.showMoveHints;
            vibrationEnabled = data.vibrationEnabled;
            ruleSetIndex = data.ruleSetIndex;
            botDifficulty = data.botDifficulty;
        }
#endif

        yield break;
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

    public RuleSetSO GetRuleSet(int index)
    {
        return ruleSetList.ruleSets[index];
    }

    public int GetRuleSetIndex()
    {
        return ruleSetIndex;
    }

    public void SetRuleSetIndex(int index)
    {
        ruleSetIndex = index;
        Save();
    }

    public BotDifficulty GetBotDifficulty()
    {
        return botDifficulty;
    }

    public void SetBotDifficulty(BotDifficulty value)
    {
        botDifficulty = value;
        Save();
    }

    private void Save()
    {
#if UNITY_ANDROID || UNITY_STANDALONE_WIN || UNITY_EDITOR
        GameSettingsData data = new()
        {
            showMoveHints = showMoveHints,
            vibrationEnabled = vibrationEnabled,
            ruleSetIndex = ruleSetIndex,
            botDifficulty = botDifficulty
        };

        SavingSystem.Save(GameSettingsData.FileName, data);
#endif
    }
}
