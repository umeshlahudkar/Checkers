using System;
using TMPro;
using UnityEngine;

public class RuleSetCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private int ruleSetIndex;
    private Action<int> onSelected;

    public void Setup(int index, RuleSetSO ruleSet, Action<int> onRuleSetSelected)
    {
        ruleSetIndex = index;
        onSelected = onRuleSetSelected;

        nameText.text = ruleSet.DisplayName;
        descriptionText.text = ruleSet.Description;
    }

    public void OnCardClick()
    {
        onSelected?.Invoke(ruleSetIndex);
    }
}
