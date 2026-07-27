using TMPro;
using UnityEngine;

// Overlay popup showing a ruleset's full rules text (RuleSetSO.LongDescription) - opened from
// RuleSetCard's "view rules" button on the mode-selection carousel. Follows the same
// Show(...)-then-OpenPageAsOverlay(...) pattern as ResultPage, just with no win/loss branching.
public class RuleSetInfoPage : Page
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    public void Show(RuleSetSO ruleSet)
    {
        titleText.text = ruleSet.DisplayName;

        // Rich-text TMP tags (<b>Header</b>) are already baked into LongDescription - Rich Text
        // must be enabled on bodyText in the Inspector for these to render instead of showing
        // literally.
        bodyText.text = ruleSet.LongDescription;
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.RuleSetInfoPage);
    }
}
