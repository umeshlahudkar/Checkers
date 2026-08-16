using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefeatPage : Page
{
    [Header("Avatars")]
    [SerializeField] private Image localAvatarImage;
    [SerializeField] private Image opponentAvatarImage;

    [Header("Rematch (PrimaryButton's own Image - see CustomButton)")]
    [SerializeField] private Image rematchButtonImage;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI localPiecesText;
    [SerializeField] private TextMeshProUGUI opponentPiecesText;
    [SerializeField] private TextMeshProUGUI reasonText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI capturesText;
    [SerializeField] private TextMeshProUGUI kingsCrownedText;
    [SerializeField] private TextMeshProUGUI matchTimeText;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image iconCircleImage;
    [SerializeField] private Image innerIconImage;
    [SerializeField] private Image titleImage;
    [SerializeField] private Image leftRuleImage;
    [SerializeField] private Image rightRuleImage;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Result Content Animation")]
    [SerializeField] private Image localAvatarFrame;
    [SerializeField] private Image opponentAvatarFrame;
    [SerializeField] private TextMeshProUGUI localNameText;
    [SerializeField] private TextMeshProUGUI piecesLabel;
    [SerializeField] private Image statPanelBackground;
    [SerializeField] private CanvasGroup capturesRow;
    [SerializeField] private CanvasGroup kingsCrownedRow;
    [SerializeField] private CanvasGroup reasonRow;
    [SerializeField] private CanvasGroup matchTimeRow;
    [SerializeField] private RectTransform mainMenuButtonTransform;
    [SerializeField] private RectTransform iconButtonTransform;

    private const float BgFadeDuration = 0.3f;
    private const float IconDropDistance = 50f;
    private const float IconDuration = 0.45f;
    private const float IconDelay = 0.05f;
    private const float TitleDuration = 0.4f;
    private const float TitleDelay = 0.35f;
    private const float RuleSubtitleDuration = 0.35f;
    private const float RuleSubtitleDelay = 0.6f;

    private const float ContentDelay = 0.8f;
    private const float ContentStagger = 0.15f;

    private const float AvatarSlideDistance = 90f;
    private const float AvatarDuration = 0.4f;

    private const float StatBgDuration = 0.35f;
    private const float StatShiftDistance = 130f;
    private const float StatRowDuration = 0.35f;
    private const float StatRowStagger = 0.1f;

    private const float ButtonSlideDistance = 700f;
    private const float ButtonDuration = 0.5f;

    private const float ShakeDuration = 0.35f;
    private const float ShakeStrength = 14f;
    private const int ShakeVibrato = 20;

    // The Bg's authored color, cached once at startup rather than re-read at animation time - the
    // "impact flash" below overwrites Bg's RGB (not just alpha) every reveal, so re-reading a possibly
    // mid-tween color as the "rest" value would drift the tint further red on repeated opens.
    private Color bgRestColor;

    private void Awake()
    {
        bgRestColor = bgImage.color;
    }

    public void Show(GameResult result)
    {
        localAvatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        opponentAvatarImage.sprite = result.OpponentAvatar;
        opponentNameText.text = result.OpponentName;
        localPiecesText.text = result.LocalPiecesLeft.ToString();
        opponentPiecesText.text = result.OpponentPiecesLeft.ToString();
        reasonText.text = result.Reason;

        capturesText.text = result.LocalCaptures.ToString();
        kingsCrownedText.text = result.LocalKingsCrowned.ToString();
        matchTimeText.text = result.MatchDuration;

        // Defensive reset - a previous match on this same page instance may have disabled the
        // button after a declined rematch offer (see SetRematchButtonInteractable).
        SetRematchButtonInteractable(true);
    }

    public void OnTryAgainButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager.GameMode == GameModeType.Multiplayer)
        {
            gameManager.OfferRematch();
        }
        else
        {
            gameManager.StartRematch();
        }
    }

    // CustomButton (unlike a standard UI Button) has no built-in disabled state - gating the
    // button's own Image's raycast target is what actually stops IPointerDown/IPointerUp/OnClick
    // from ever reaching it (see CustomButton, and GamePage.SetOfferDrawButtonClickable for the same
    // trick applied to the Offer Draw button).
    public void SetRematchButtonInteractable(bool interactable)
    {
        rematchButtonImage.raycastTarget = interactable;
    }

    public void OnMainMenuButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        ServiceLocator.Get<GameManager>().GoToMainMenu();
    }

    public void OnRulesButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameSettingsManager gameSettings = ServiceLocator.Get<GameSettingsManager>();
        ServiceLocator.Get<GamePageManager>().RuleSetInfoPage.Show(ServiceLocator.Get<GameManager>().RuleSet, gameSettings.GetRuleSetIndex(), gameSettings.RuleSetCount);
        ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.RuleSetInfoPage);
    }

    protected override void OnOpened()
    {
        PlayEntranceAnimation();
    }

    // Heavier, "deflated" reveal: the whole page shakes like an impact the instant it opens, the Bg
    // flashes red before settling to its normal tint, and the icon falls into place instead of
    // popping - everything else eases out firmly (Ease.OutQuad, no overshoot) rather than bouncing
    // like Victory's, so a loss reads as a jarring gut-punch instead of a fancier fade-in.
    private void PlayEntranceAnimation()
    {
        RectTransform pageTransform = (RectTransform)transform;
        DOTween.Kill(pageTransform);
        pageTransform.DOShakeAnchorPos(ShakeDuration, ShakeStrength, ShakeVibrato, 90f, false, true);

        DOTween.Kill(bgImage);
        bgImage.color = new Color(0.85f, 0.16f, 0.16f, 0f);
        bgImage.DOColor(bgRestColor, BgFadeDuration * 1.6f).SetEase(Ease.OutQuad);

        iconCircleImage.DropIn(IconDropDistance, IconDuration, IconDelay, Ease.InQuad);
        innerIconImage.DropIn(IconDropDistance, IconDuration, IconDelay, Ease.InQuad);

        titleImage.PopIn(TitleDuration, TitleDelay, Ease.OutQuad);

        leftRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutQuad);
        rightRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutQuad);
        subtitleText.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutQuad);

        PlayResultContentAnimation();
    }

    // Avatars slide in from their own side while fading in (local from the left, opponent from the
    // right). The stat panel then arrives in two beats instead of all at once: its background (and
    // divider lines) fade in first as an empty container, then each stat row - label+value grouped as
    // a single rigid unit so they can never drift apart - slides in from the left while fading, one
    // row after another rather than all four together. Buttons rise in last, entering from off the
    // bottom of the screen - all still the same firm Ease.OutQuad (no overshoot) as the rest of this
    // page's reveal.
    private void PlayResultContentAnimation()
    {
        float avatarDelay = ContentDelay;
        float statBgDelay = avatarDelay + ContentStagger;
        float rowsDelay = statBgDelay + ContentStagger;
        float lastRowDelay = rowsDelay + StatRowStagger * 3f;
        float buttonDelay = lastRowDelay + ContentStagger;

        localAvatarFrame.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);
        localAvatarImage.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);
        localNameText.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);

        opponentAvatarFrame.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);
        opponentAvatarImage.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);
        opponentNameText.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutQuad);

        localPiecesText.FadeIn(AvatarDuration, avatarDelay);
        opponentPiecesText.FadeIn(AvatarDuration, avatarDelay);
        piecesLabel.FadeIn(AvatarDuration, avatarDelay);

        statPanelBackground.FadeIn(StatBgDuration, statBgDelay);
        FadeInStatDividers(StatBgDuration, statBgDelay);

        Vector2 statOffset = new Vector2(-StatShiftDistance, 0f);
        capturesRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay, Ease.OutQuad);
        kingsCrownedRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger, Ease.OutQuad);
        reasonRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger * 2f, Ease.OutQuad);
        matchTimeRow.SlideFadeInGroup(statOffset, StatRowDuration, lastRowDelay, Ease.OutQuad);

        rematchButtonImage.rectTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutQuad);
        mainMenuButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutQuad);
        iconButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutQuad);
    }

    // Divider lines aren't wired as individual fields - found by name under the stat panel instead,
    // since they're purely decorative separators with nothing else that needs to reference them.
    private void FadeInStatDividers(float duration, float delay)
    {
        Transform panelTransform = statPanelBackground.transform;
        for (int i = 0; i < panelTransform.childCount; i++)
        {
            Transform child = panelTransform.GetChild(i);
            if (child.name == "Divider")
            {
                Image dividerImage = child.GetComponent<Image>();
                if (dividerImage != null)
                {
                    dividerImage.FadeIn(duration, delay);
                }
            }
        }
    }
}
