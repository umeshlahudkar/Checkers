using System;
using DG.Tweening;
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

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform contentTransform;

    private const float BgFadeInDuration = 0.2f;
    private const float ContentPopInDuration = 0.3f;
    private const float BgFadeOutDuration = 0.15f;
    private const float ContentPopOutDuration = 0.2f;

    // Bg is authored as a partly-transparent dim overlay (not fully opaque, same as
    // ConfirmationPopup's Bg) - its rest alpha is cached once here rather than assumed to be 1, since
    // fading to 1 would leave it fully opaque instead of dimming whatever's behind the popup.
    private float bgRestAlpha;

    // Guards against a second click (CloseButton and GotItButton both land here) firing the close
    // sequence twice while the exit animation is still shrinking the panel. Reset every time the page
    // opens.
    private bool isClosing;

    private void Awake()
    {
        bgRestAlpha = bgImage.color.a;
    }

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
        isClosing = false;
        PlayEntranceAnimation();

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

    // Same quick pop-in as ConfirmationPopup: the dim backdrop fades in to its authored (not full)
    // alpha while the panel scales up from nothing with a slight overshoot.
    private void PlayEntranceAnimation()
    {
        DOTween.Kill(bgImage);
        Color bgColor = bgImage.color;
        bgImage.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0f);
        bgImage.DOFade(bgRestAlpha, BgFadeInDuration);

        DOTween.Kill(contentTransform);
        contentTransform.localScale = Vector3.zero;
        contentTransform.DOScale(1f, ContentPopInDuration).SetEase(Ease.OutBack);
    }

    // Mirror of the pop-in (shrink with Ease.InBack) played BEFORE actually closing - Page.Close
    // disables the GameObject immediately (see Page.cs), so unlike OnOpened there's no usable
    // OnClosed hook to animate from; the close has to wait until this tween's OnComplete instead.
    private void PlayExitAnimation(Action onComplete)
    {
        DOTween.Kill(bgImage);
        bgImage.DOFade(0f, BgFadeOutDuration);

        DOTween.Kill(contentTransform);
        contentTransform.DOScale(0f, ContentPopOutDuration).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }

    // Only one of MenuPageManager/GamePageManager is ever registered at a time (each is scene-
    // scoped, and ServiceLocator is cleared on every scene load) - whichever one is present here
    // is necessarily the one that opened this instance, since only its scene has this page
    // registered at all.
    public void OnCloseButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            if (ServiceLocator.TryGet(out MenuPageManager menuPageManager))
            {
                menuPageManager.CloseOverlay(MenuPageType.RuleSetInfoPage);
            }
            else
            {
                ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.RuleSetInfoPage);
            }
        });
    }
}
