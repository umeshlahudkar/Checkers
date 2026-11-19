using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettingsManager : Service<GameSettingsManager>, IInitializable
{
    [SerializeField] private RuleSetListSO ruleSetList;

    private bool showMoveHints;
    private bool vibrationEnabled;
    private int ruleSetIndex;
    private BotDifficulty botDifficulty;

    // Positions in ruleSetList of the rulesets that are switched on (RuleSetSO.AvailableInGame).
    private readonly List<int> availableIndices = new List<int>();

    public bool ShowMoveHints { get { return showMoveHints; } }
    public bool VibrationEnabled { get { return vibrationEnabled; } }

    // Rulesets are identified by their position in the RuleSetList - that number is what gets saved
    // and what other players are sent - so "index" always means that position, switched on or not.
    // The match setup popup offers only the switched-on ones, by their place among those (see
    // AvailableRuleSetCount and GetAvailablePosition).
    public int RuleSetCount { get { return ruleSetList.ruleSets.Count; } }
    public int AvailableRuleSetCount { get { return availableIndices.Count; } }

    public IEnumerator Initialize()
    {
        showMoveHints = true;
        vibrationEnabled = true;
        ruleSetIndex = 0;
        botDifficulty = BotDifficulty.Easy;

        FindAvailableRuleSets();

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

        // The saved choice may have been switched off (or taken out of the list) since it was saved.
        if (!availableIndices.Contains(ruleSetIndex))
        {
            ruleSetIndex = availableIndices[0];
        }

        yield break;
    }

    private void FindAvailableRuleSets()
    {
        availableIndices.Clear();

        for (int i = 0; i < ruleSetList.ruleSets.Count; i++)
        {
            RuleSetSO ruleSet = ruleSetList.ruleSets[i];

            if (ruleSet != null && ruleSet.AvailableInGame)
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count > 0)
        {
            return;
        }

        // Nothing switched on would leave the popup with nothing to pick, so show everything instead.
        Debug.LogError("[GameSettingsManager] Every ruleset is switched off (RuleSetSO.AvailableInGame). Showing all of them.");

        for (int i = 0; i < ruleSetList.ruleSets.Count; i++)
        {
            availableIndices.Add(i);
        }
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

    // Where a ruleset sits among the switched-on ones, or -1 when it is not one of them (for example
    // a friend's room that uses a ruleset this build has switched off).
    public int GetAvailablePosition(int index)
    {
        return availableIndices.IndexOf(index);
    }

    public int GetAvailablePosition(IRuleSet ruleSet)
    {
        for (int position = 0; position < availableIndices.Count; position++)
        {
            if (ReferenceEquals(ruleSetList.ruleSets[availableIndices[position]], ruleSet))
            {
                return position;
            }
        }

        return -1;
    }

    // The ruleset index of the ruleset at a place among the switched-on ones.
    public int GetIndexAtAvailablePosition(int position)
    {
        return availableIndices[position];
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
