using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay popup shown between tapping a mode tile and the match actually starting: a "match setup"
// card that lines the player up against whoever they are about to face, then lists the handful of
// rules that change how a game feels. Start begins the match; Back and the close X both back out.
//
// Same Show(...)-then-OpenPageAsOverlay(...) shape as RuleSetInfoPage, and the same DOScale/DOFade
// entrance that ConfirmationPopup and WarningNotifier use, so popups across the app materialise the
// same way. Distinct from ConfirmationPopup (a generic yes/no prompt of header + one description)
// because this one lists structured match detail rather than prose.
public class MatchConfirmPage : Page
{
    [Header("Lineup")]
    [SerializeField] private Image opponentIconImage;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI opponentSubText;

    [Header("Rules Table")]
    [SerializeField] private TextMeshProUGUI rulesValueText;
    [SerializeField] private TextMeshProUGUI piecesValueText;
    [SerializeField] private TextMeshProUGUI kingsValueText;
    [SerializeField] private TextMeshProUGUI captureValueText;
    [SerializeField] private TextMeshProUGUI menCaptureValueText;

    // The confirm button's label: "START" for most modes, but creating a private room isn't starting
    // a match yet - it opens a room and waits for a friend - so that mode says what it does.
    [Header("Confirm Button")]
    [SerializeField] private TextMeshProUGUI nextButtonText;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform contentTransform;

    private const float BgFadeInDuration = 0.2f;
    private const float ContentPopInDuration = 0.3f;
    private const float BgFadeOutDuration = 0.15f;
    private const float ContentPopOutDuration = 0.2f;

    // Bg is a partly-transparent dim overlay, so its rest alpha is cached once rather than assumed
    // to be 1 - same reason as ConfirmationPopup.bgRestAlpha.
    private float bgRestAlpha;

    // Start and Back/close all land here, and the panel stays clickable while the exit tween is
    // still shrinking it, so a second click must not start the match twice. Reset on every open.
    private bool isClosing;

    private Action onConfirmed;

    private string defaultNextButtonText;

    private void Awake()
    {
        bgRestAlpha = bgImage.color.a;
    }

    // modeDetail is the tile's own sub-choice where it has one (the bot's difficulty); it replaces
    // the opponent's default sub-label.
    public void Show(ModeInfo info, RuleSetSO ruleSet, string modeDetail, Action onNext)
    {
        onConfirmed = onNext;

        ShowOpponent(info, modeDetail);
        ShowRules(ruleSet);

        // Read here, not in Awake: the popup is inactive until first opened, so Awake only runs after
        // the first Show - by which point the label could already have been changed.
        if (defaultNextButtonText == null)
        {
            defaultNextButtonText = nextButtonText.text;
        }

        nextButtonText.text = info.mode == GameModeType.PrivateRoom ? "CREATE ROOM" : defaultNextButtonText;
    }

    private void ShowOpponent(ModeInfo info, string modeDetail)
    {
        opponentIconImage.sprite = info.modeIcon;

        string opponentName;
        string defaultSub;
        switch (info.mode)
        {
            case GameModeType.VsBot:
                opponentName = "Bot";
                defaultSub = string.Empty;
                break;
            case GameModeType.VsPlayer:
                opponentName = "Player 2";
                defaultSub = "SAME DEVICE";
                break;
            case GameModeType.Multiplayer:
                opponentName = "Opponent";
                defaultSub = "ONLINE";
                break;
            case GameModeType.PrivateRoom:
                opponentName = "Friend";
                defaultSub = "PRIVATE ROOM";
                break;
            default:
                opponentName = info.modeName;
                defaultSub = string.Empty;
                break;
        }

        opponentNameText.text = opponentName;
        opponentSubText.text = string.IsNullOrEmpty(modeDetail) ? defaultSub : modeDetail.ToUpperInvariant();
    }

    // Only the rules that actually change how a game plays - board, material, kings and capture
    // obligation. The full prose lives in RuleSetInfoPage; this is the at-a-glance version.
    private void ShowRules(RuleSetSO ruleSet)
    {
        rulesValueText.text = $"{ruleSet.DisplayName}  ·  {ruleSet.Rows}×{ruleSet.Columns}";
        piecesValueText.text = $"{ruleSet.PiecesPerSide} each";
        kingsValueText.text = ruleSet.FlyingKings ? "Flying kings" : "No flying kings";
        captureValueText.text = ruleSet.MustCaptureMaximum ? "Longest forced" : "Any capture";
        menCaptureValueText.text = ruleSet.MenCaptureBackward ? "Forward and backward" : "Forward only";
    }

    public void OnNextButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.MatchConfirm);
            onConfirmed?.Invoke();
        });
    }

    public void OnCloseButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<MenuPageManager>().CloseOverlay(MenuPageType.MatchConfirm);
        });
    }

    public override bool OnBackPressed()
    {
        OnCloseButtonClick();
        return true;
    }

    protected override void OnOpened()
    {
        isClosing = false;

        // The rules table sizes itself through a VerticalLayoutGroup/ContentSizeFitter chain, and
        // Unity doesn't reliably re-measure that the same frame a script changes text or toggles a
        // row, so a previous mode's row count would otherwise leave the card the wrong height. Has
        // to happen here rather than in Show(): the page is still inactive at that point, and
        // ForceRebuildLayoutImmediate silently no-ops on anything inactive.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentTransform);

        PlayEntranceAnimation();
    }

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

    // Page.Close disables the GameObject immediately, so the close has to wait on this tween's
    // OnComplete rather than animate from an OnClosed hook - same as ConfirmationPopup.
    private void PlayExitAnimation(Action onComplete)
    {
        DOTween.Kill(bgImage);
        bgImage.DOFade(0f, BgFadeOutDuration);

        DOTween.Kill(contentTransform);
        contentTransform.DOScale(0f, ContentPopOutDuration).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }
}
