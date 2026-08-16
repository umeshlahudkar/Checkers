using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrawPage : Page
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

    private const float BgFadeDuration = 0.25f;
    private const float IconDuration = 0.5f;
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

    // Decaying oscillation (each swing smaller than the last, ending back at 0) applied to the
    // balance-scale icon once it's popped in - reads as the scale physically rocking to equilibrium,
    // a motion unique to Draw that neither Victory's spin nor Defeat's shake/drop resembles.
    private static readonly float[] SwingAngles = new float[] { 20f, -14f, 9f, -5f, 2f, 0f };
    private const float SwingStepDuration = 0.22f;
    private Sequence swingSequence;

    public void Show(GameResult result)
    {
        localAvatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        opponentAvatarImage.sprite = result.OpponentAvatar;
        opponentNameText.text = result.OpponentName;
        localPiecesText.text = result.LocalPiecesLeft.ToString();
        opponentPiecesText.text = result.OpponentPiecesLeft.ToString();
        reasonText.text = result.Reason;

        capturesText.text = $"{result.LocalCaptures} : {result.OpponentCaptures}";
        kingsCrownedText.text = $"{result.LocalKingsCrowned} : {result.OpponentKingsCrowned}";
        matchTimeText.text = result.MatchDuration;

        // Defensive reset - a previous match on this same page instance may have disabled the
        // button after a declined rematch offer (see SetRematchButtonInteractable).
        SetRematchButtonInteractable(true);
    }

    public void OnPlayAgainButtonClick()
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

    public void OnShareButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
    }

    protected override void OnOpened()
    {
        PlayEntranceAnimation();
    }

    protected override void OnClosed()
    {
        swingSequence?.Kill();
    }

    // Calm, even-handed reveal: everything eases in gently (Ease.OutSine, no bounce, no drop) since a
    // draw is neither a win nor a loss, then the balance-scale icon physically rocks and settles to
    // equilibrium - a distinct, thematic motion neither Victory's burst nor Defeat's shake/fall shares.
    private void PlayEntranceAnimation()
    {
        DOTween.Kill(bgImage);
        bgImage.color = new Color(bgImage.color.r, bgImage.color.g, bgImage.color.b, 0f);
        bgImage.DOFade(1f, BgFadeDuration);

        iconCircleImage.PopIn(IconDuration, IconDelay, Ease.OutSine);
        innerIconImage.PopIn(IconDuration, IconDelay, Ease.OutSine);

        swingSequence?.Kill();
        innerIconImage.rectTransform.localRotation = Quaternion.identity;
        swingSequence = DOTween.Sequence();
        swingSequence.SetDelay(IconDelay + IconDuration);
        foreach (float angle in SwingAngles)
        {
            swingSequence.Append(innerIconImage.rectTransform.DORotate(new Vector3(0f, 0f, angle), SwingStepDuration).SetEase(Ease.InOutSine));
        }

        titleImage.PopIn(TitleDuration, TitleDelay, Ease.OutSine);

        leftRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutSine);
        rightRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutSine);
        subtitleText.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutSine);

        PlayResultContentAnimation();
    }

    // Avatars slide in from their own side while fading in (local from the left, opponent from the
    // right). The stat panel then arrives in two beats instead of all at once: its background (and
    // divider lines) fade in first as an empty container, then each stat row - label+value grouped as
    // a single rigid unit so they can never drift apart - slides in from the left while fading, one
    // row after another rather than all four together. Buttons rise in last, entering from off the
    // bottom of the screen - all still the same gentle Ease.OutSine as the rest of this page's
    // reveal, so it stays calm.
    private void PlayResultContentAnimation()
    {
        float avatarDelay = ContentDelay;
        float statBgDelay = avatarDelay + ContentStagger;
        float rowsDelay = statBgDelay + ContentStagger;
        float lastRowDelay = rowsDelay + StatRowStagger * 3f;
        float buttonDelay = lastRowDelay + ContentStagger;

        localAvatarFrame.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);
        localAvatarImage.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);
        localNameText.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);

        opponentAvatarFrame.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);
        opponentAvatarImage.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);
        opponentNameText.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutSine);

        localPiecesText.FadeIn(AvatarDuration, avatarDelay);
        opponentPiecesText.FadeIn(AvatarDuration, avatarDelay);
        piecesLabel.FadeIn(AvatarDuration, avatarDelay);

        statPanelBackground.FadeIn(StatBgDuration, statBgDelay);
        FadeInStatDividers(StatBgDuration, statBgDelay);

        Vector2 statOffset = new Vector2(-StatShiftDistance, 0f);
        capturesRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay, Ease.OutSine);
        kingsCrownedRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger, Ease.OutSine);
        reasonRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger * 2f, Ease.OutSine);
        matchTimeRow.SlideFadeInGroup(statOffset, StatRowDuration, lastRowDelay, Ease.OutSine);

        rematchButtonImage.rectTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutSine);
        mainMenuButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutSine);
        iconButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutSine);
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
