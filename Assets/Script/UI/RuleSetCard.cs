using System;
using TMPro;
using UnityEngine;

public class RuleSetCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Meta Chips")]
    [SerializeField] private TextMeshProUGUI boardSizeChipText;
    [SerializeField] private TextMeshProUGUI pieceCountChipText;
    [SerializeField] private TextMeshProUGUI flyingKingsChipText;

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

        boardSizeChipText.text = $"{ruleSet.Rows}×{ruleSet.Columns} BOARD";
        pieceCountChipText.text = $"{ruleSet.PieceRowsPerSide * (ruleSet.Columns / 2)} PIECES";
        flyingKingsChipText.text = ruleSet.FlyingKings ? "FLYING KINGS" : "NO FLYING KINGS";
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
