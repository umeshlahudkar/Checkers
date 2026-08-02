using TMPro;
using UnityEngine;

// Overlay popup showing a ruleset's full rules text (RuleSetSO.LongDescription) - reused from two
// different places with two different page managers: RuleSetCard's "view rules" button on the
// mode-selection carousel (MenuPageManager, mode-selection scene) and GamePage.OnRulesButtonClick
// (GamePageManager, gameplay scene). Follows the same Show(...)-then-OpenPageAsOverlay(...)
// pattern as ResultPage, just with no win/loss branching.
public class RuleSetInfoPage : Page
{
    [SerializeField] private TextMeshProUGUI variantLabelText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    public void Show(IRuleSet ruleSet, int ruleSetIndex, int ruleSetCount)
    {
        variantLabelText.text = $"RULES VARIANT {ruleSetIndex + 1} OF {ruleSetCount}";
        titleText.text = ruleSet.DisplayName;

        // Rich-text TMP tags (<b>Header</b>) are already baked into LongDescription - Rich Text
        // must be enabled on bodyText in the Inspector for these to render instead of showing
        // literally.
        bodyText.text = ruleSet.LongDescription;
    }

    // Only one of MenuPageManager/GamePageManager is ever registered at a time (each is scene-
    // scoped, and ServiceLocator is cleared on every scene load) - whichever one is present here
    // is necessarily the one that opened this instance, since only its scene has this page
    // registered at all.
    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if (ServiceLocator.TryGet(out MenuPageManager menuPageManager))
        {
            menuPageManager.CloseOverlay(MenuPageType.RuleSetInfoPage);
        }
        else
        {
            ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.RuleSetInfoPage);
        }
    }
}
