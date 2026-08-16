using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Generic Accept/Decline overlay popup - set header/description/button labels and the two callbacks
// at runtime via Show(...), then ServiceLocator.Get<DDOLPageManager>().OpenPageAsOverlay(DDOLPageType.
// ConfirmationPopup). Lives under DDOL_Canvas (see DDOLPageManager, alongside FaderPage/LoadingPage)
// rather than GamePageManager's per-scene page set, so it's reachable from any scene - main menu,
// matchmaking, gameplay - not just mid-match. Replaces what used to be three separate pages
// (DrawOfferPage/RematchOfferPage, then QuitPage) - see GameManager.ReceiveDrawOffer/
// ReceiveRematchOffer and GamePage.OnHomeButtonClick/OnRestartButtonClick for the current callers.
// Any future yes/no confirmation of the same shape reuses this instead of a new page/prefab.
//
// The popup closes itself on either button before invoking the callback, so callers only need to
// supply what should happen on accept/decline, not the page-closing itself.
public class ConfirmationPopup : Page
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI acceptButtonText;
    [SerializeField] private TextMeshProUGUI declineButtonText;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private RectTransform contentTransform;

    private const float BgFadeInDuration = 0.2f;
    private const float ContentPopInDuration = 0.3f;
    private const float BgFadeOutDuration = 0.15f;
    private const float ContentPopOutDuration = 0.2f;

    // Bg is authored as a partly-transparent dim overlay (not fully opaque), so its rest alpha has to
    // be captured once here rather than assumed to be 1 - fading to 1 (as the shared Graphic.FadeIn
    // helper does) would leave it fully opaque, hiding whatever's behind the popup instead of dimming
    // it. Cached once at Awake rather than re-read at animation time for the same reason as
    // DefeatPage.bgRestColor: re-reading a possibly mid-tween alpha as "rest" would drift on repeated
    // opens (this popup is a single reused instance across many draw/rematch offers in a match).
    private float bgRestAlpha;

    // Guards against a second button click landing while the exit animation is still shrinking the
    // dialog (it's still visible, just smaller/fainter, so it can still be clicked) from firing
    // onAccept/onDecline twice or double-closing. Reset every time the popup opens.
    private bool isClosing;

    private Action onAccept;
    private Action onDecline;

    private void Awake()
    {
        bgRestAlpha = bgImage.color.a;
    }

    public void Show(string header, string description, string acceptLabel, string declineLabel, Action onAccept, Action onDecline)
    {
        headerText.text = header;
        descriptionText.text = description;
        acceptButtonText.text = acceptLabel;
        declineButtonText.text = declineLabel;
        this.onAccept = onAccept;
        this.onDecline = onDecline;
    }

    public void OnAcceptButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<DDOLPageManager>().CloseOverlay(DDOLPageType.ConfirmationPopup);
            onAccept?.Invoke();
        });
    }

    public void OnDeclineButtonClick()
    {
        if (isClosing) { return; }
        isClosing = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PlayExitAnimation(() =>
        {
            ServiceLocator.Get<DDOLPageManager>().CloseOverlay(DDOLPageType.ConfirmationPopup);
            onDecline?.Invoke();
        });
    }

    protected override void OnOpened()
    {
        isClosing = false;
        PlayEntranceAnimation();
    }

    // Quick, punchy pop-in (the same DOScale/Ease.OutBack treatment WarningNotifier already uses for
    // its own toasts) - the dim backdrop fades in while the dialog box scales up from nothing, so a
    // confirmation prompt reads as materializing rather than just snapping into place.
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

    // Mirror of the pop-in (shrink with Ease.InBack, the same anticipation-style curve WarningNotifier
    // uses for its own pop-out) played BEFORE actually closing - Page.Close disables the GameObject
    // immediately (see Page.cs), so unlike OnOpened there's no usable OnClosed hook to animate from;
    // the close (and the accept/decline callback) has to wait until this tween's OnComplete instead.
    private void PlayExitAnimation(Action onComplete)
    {
        DOTween.Kill(bgImage);
        bgImage.DOFade(0f, BgFadeOutDuration);

        DOTween.Kill(contentTransform);
        contentTransform.DOScale(0f, ContentPopOutDuration).SetEase(Ease.InBack)
            .OnComplete(() => onComplete?.Invoke());
    }
}
