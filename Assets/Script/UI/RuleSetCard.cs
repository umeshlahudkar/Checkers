using System;
using TMPro;
using UnityEngine;

public class RuleSetCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private int ruleSetIndex;
    private Action<int> onSelected;
    private RuleSetSO currentRuleSet;

    public void Setup(int index, RuleSetSO ruleSet, Action<int> onRuleSetSelected)
    {
        ruleSetIndex = index;
        onSelected = onRuleSetSelected;
        currentRuleSet = ruleSet;

        nameText.text = ruleSet.DisplayName;
        descriptionText.text = ruleSet.Description;
    }

    public void OnCardClick()
    {
        onSelected?.Invoke(ruleSetIndex);
    }

    public void OnViewRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().RuleSetInfoPage.Show(currentRuleSet);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.RuleSetInfoPage);
    }
}
