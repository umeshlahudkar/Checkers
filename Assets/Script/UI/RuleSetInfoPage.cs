using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay popup showing a ruleset's full rules text (RuleSetSO.LongDescription) - reused from two
// different places with two different page managers: RuleSetCard's "view rules" button on the
// mode-selection carousel (MenuPageManager, mode-selection scene) and GamePage.OnRulesButtonClick
// (GamePageManager, gameplay scene). Follows the same Show(...)-then-OpenPageAsOverlay(...)
// pattern as VictoryPage/DefeatPage/DrawPage, just with no win/loss branching.
public class RuleSetInfoPage : Page
{
    [SerializeField] private TextMeshProUGUI variantLabelText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private ScrollRect bodyScrollRect;

    public void Show(IRuleSet ruleSet, int ruleSetIndex, int ruleSetCount)
    {
        variantLabelText.text = $"RULES VARIANT {ruleSetIndex + 1} OF {ruleSetCount}";
        titleText.text = ruleSet.DisplayName;

        // Rich-text TMP tags (<b>Header</b>) are already baked into LongDescription - Rich Text
        // must be enabled on bodyText in the Inspector for these to render instead of showing
        // literally.
        bodyText.text = ruleSet.LongDescription;
    }

    // Called from every OpenPageAsOverlay call site right after Show(...) - see Page.Open(), which
    // activates the GameObject before calling this. The rebuild below has to happen here rather
    // than in Show() itself: at the moment Show() runs the page is still inactive (Open() hasn't
    // SetActive(true)'d it yet), and both Canvas.ForceUpdateCanvases and
    // LayoutRebuilder.ForceRebuildLayoutImmediate silently no-op on anything not active in the
    // hierarchy - so calling them from Show() never actually rebuilt anything.
    protected override void OnOpened()
    {
        // bodyScrollRect.content sits under a VerticalLayoutGroup/ContentSizeFitter chain that
        // measures off bodyText's height - Unity doesn't always re-measure that same frame a
        // script changes .text (same gap ModeSelectionPage.RefreshLayout works around for its own
        // scroll content), so a shorter ruleset's stale height can silently cap how far this one
        // scrolls. Forcing the rebuild, then resetting to the top, makes every open start correct
        // regardless of how long the previous ruleset's text was.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(bodyScrollRect.content);
        bodyScrollRect.verticalNormalizedPosition = 1f;
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
