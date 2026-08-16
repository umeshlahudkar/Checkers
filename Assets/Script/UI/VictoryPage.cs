using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VictoryPage : Page
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

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI capturesText;
    [SerializeField] private TextMeshProUGUI kingsCrownedText;
    [SerializeField] private TextMeshProUGUI longestChainText;
    [SerializeField] private TextMeshProUGUI matchTimeText;

    [Header("Entrance Animation")]
    [SerializeField] private Image bgImage;
    [SerializeField] private Image rayBurstImage;
    [SerializeField] private Image crownIconImage;
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
    [SerializeField] private CanvasGroup longestChainRow;
    [SerializeField] private CanvasGroup matchTimeRow;
    [SerializeField] private RectTransform mainMenuButtonTransform;
    [SerializeField] private RectTransform iconButtonTransform;

    private const float BgFadeDuration = 0.2f;
    private const float IconDuration = 0.45f;
    private const float IconDelay = 0.05f;
    private const float RayBurstSpinAngle = 50f;
    private const float TitleDuration = 0.35f;
    private const float TitleDelay = 0.3f;
    private const float RuleSubtitleDuration = 0.3f;
    private const float RuleSubtitleDelay = 0.5f;

    private const float ContentDelay = 0.7f;
    private const float ContentStagger = 0.15f;

    private const float AvatarSlideDistance = 90f;
    private const float AvatarDuration = 0.35f;

    private const float StatBgDuration = 0.3f;
    private const float StatShiftDistance = 130f;
    private const float StatRowDuration = 0.3f;
    private const float StatRowStagger = 0.1f;

    private const float ButtonSlideDistance = 700f;
    private const float ButtonDuration = 0.5f;

    // Kept spinning slowly for as long as the page stays open (unlike every other tween here, which
    // is a one-shot reveal) - gives Victory an ongoing "shimmer" so it keeps feeling alive while the
    // player reads their stats, instead of going fully static like Defeat/Draw.
    private const float RayBurstLoopDuration = 6f;
    private Tween rayBurstLoopTween;

    private static Sprite confettiPixelSprite;
    private static readonly Color[] ConfettiPalette = new Color[]
    {
        new Color(1f, 0.85f, 0.2f),
        new Color(1f, 1f, 1f),
        new Color(1f, 0.55f, 0.15f),
        new Color(0.95f, 0.35f, 0.35f),
    };

    private const int ConfettiPieceCount = 26;
    private const float ConfettiSpreadX = 320f;
    private const float ConfettiFallDistanceMin = 550f;
    private const float ConfettiFallDistanceMax = 750f;
    private const float ConfettiFallDurationMin = 0.9f;
    private const float ConfettiFallDurationMax = 1.4f;
    private const float ConfettiSpawnStagger = 0.35f;

    public void Show(GameResult result)
    {
        localAvatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        opponentAvatarImage.sprite = result.OpponentAvatar;
        opponentNameText.text = result.OpponentName;
        localPiecesText.text = result.LocalPiecesLeft.ToString();
        opponentPiecesText.text = result.OpponentPiecesLeft.ToString();

        capturesText.text = result.LocalCaptures.ToString();
        kingsCrownedText.text = result.LocalKingsCrowned.ToString();
        longestChainText.text = result.LocalLongestChain.ToString();
        matchTimeText.text = result.MatchDuration;

        // Defensive reset - a previous match on this same page instance may have disabled the
        // button after a declined rematch offer (see SetRematchButtonInteractable).
        SetRematchButtonInteractable(true);
    }

    public void OnRematchButtonClick()
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
        rayBurstLoopTween?.Kill();
    }

    // Jubilant "trophy" reveal: confetti starts falling immediately, the rays burst open and spin,
    // the crown pops with an overshoot bounce, then the title/rules/subtitle punch in the same way
    // and the stat panel rises up last - a snappier, bouncier, more colorful beat than Defeat/Draw's
    // calmer settles, so a win reads unmistakably as a celebration rather than just a fancier fade-in.
    private void PlayEntranceAnimation()
    {
        rayBurstLoopTween?.Kill();

        DOTween.Kill(bgImage);
        bgImage.color = new Color(bgImage.color.r, bgImage.color.g, bgImage.color.b, 0f);
        bgImage.DOFade(1f, BgFadeDuration);

        SpawnConfettiBurst();

        rayBurstImage.PopIn(IconDuration, IconDelay, Ease.OutQuad);
        rayBurstImage.rectTransform.localRotation = Quaternion.identity;
        rayBurstImage.rectTransform.DORotate(new Vector3(0f, 0f, RayBurstSpinAngle), IconDuration, RotateMode.FastBeyond360)
            .SetDelay(IconDelay)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                rayBurstLoopTween = rayBurstImage.rectTransform
                    .DORotate(new Vector3(0f, 0f, 360f), RayBurstLoopDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1, LoopType.Restart);
            });
        crownIconImage.PopIn(IconDuration, IconDelay, Ease.OutBack);

        titleImage.PopIn(TitleDuration, TitleDelay, Ease.OutBack);

        leftRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutBack);
        rightRuleImage.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutBack);
        subtitleText.PopIn(RuleSubtitleDuration, RuleSubtitleDelay, Ease.OutBack);

        PlayResultContentAnimation();
    }

    // Avatars slide in from their own side while fading in (local from the left, opponent from the
    // right). The stat panel then arrives in two beats instead of all at once: its background (and
    // divider lines) fade in first as an empty container, then each stat row - label+value grouped as
    // a single rigid unit so they can never drift apart - slides in from the left while fading, one
    // row after another rather than all four together. Buttons rise in last, entering from off the
    // bottom of the screen.
    private void PlayResultContentAnimation()
    {
        float avatarDelay = ContentDelay;
        float statBgDelay = avatarDelay + ContentStagger;
        float rowsDelay = statBgDelay + ContentStagger;
        float lastRowDelay = rowsDelay + StatRowStagger * 3f;
        float buttonDelay = lastRowDelay + ContentStagger;

        localAvatarFrame.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);
        localAvatarImage.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);
        localNameText.SlideFadeIn(new Vector2(-AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);

        opponentAvatarFrame.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);
        opponentAvatarImage.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);
        opponentNameText.SlideFadeIn(new Vector2(AvatarSlideDistance, 0f), AvatarDuration, avatarDelay, Ease.OutBack);

        localPiecesText.FadeIn(AvatarDuration, avatarDelay);
        opponentPiecesText.FadeIn(AvatarDuration, avatarDelay);
        piecesLabel.FadeIn(AvatarDuration, avatarDelay);

        statPanelBackground.FadeIn(StatBgDuration, statBgDelay);
        FadeInStatDividers(StatBgDuration, statBgDelay);

        Vector2 statOffset = new Vector2(-StatShiftDistance, 0f);
        capturesRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay, Ease.OutBack);
        kingsCrownedRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger, Ease.OutBack);
        longestChainRow.SlideFadeInGroup(statOffset, StatRowDuration, rowsDelay + StatRowStagger * 2f, Ease.OutBack);
        matchTimeRow.SlideFadeInGroup(statOffset, StatRowDuration, lastRowDelay, Ease.OutBack);

        rematchButtonImage.rectTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutBack);
        mainMenuButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutBack);
        iconButtonTransform.SlideUpIn(ButtonSlideDistance, ButtonDuration, buttonDelay, Ease.OutBack);
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

    // Procedurally spawns small falling rectangles tinted from a gold/white/orange palette - no
    // sprite asset needed (each piece is a 1x1 white texture recolored per-instance), so a win gets a
    // real celebration effect without any new art. Pieces self-destroy once they've fallen and faded.
    private void SpawnConfettiBurst()
    {
        if (confettiPixelSprite == null)
        {
            Texture2D texture = Texture2D.whiteTexture;
            confettiPixelSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
        }

        RectTransform pageTransform = (RectTransform)transform;
        for (int i = 0; i < ConfettiPieceCount; i++)
        {
            SpawnConfettiPiece(pageTransform);
        }
    }

    private void SpawnConfettiPiece(RectTransform parent)
    {
        GameObject pieceObject = new GameObject("ConfettiPiece", typeof(RectTransform), typeof(Image));
        RectTransform pieceTransform = (RectTransform)pieceObject.transform;
        pieceTransform.SetParent(parent, false);

        // A fresh RectTransform defaults to a bottom-left anchor/pivot, not the top-center the
        // anchoredPosition math below assumes - without this it spawns pieces off in a corner instead
        // of falling through the middle of the page.
        pieceTransform.anchorMin = new Vector2(0.5f, 1f);
        pieceTransform.anchorMax = new Vector2(0.5f, 1f);
        pieceTransform.pivot = new Vector2(0.5f, 0.5f);

        float width = Random.Range(8f, 16f);
        Vector2 startPosition = new Vector2(Random.Range(-ConfettiSpreadX, ConfettiSpreadX), 40f);
        pieceTransform.sizeDelta = new Vector2(width, width * Random.Range(0.4f, 1f));
        pieceTransform.anchoredPosition = startPosition;
        pieceTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Image pieceImage = pieceObject.GetComponent<Image>();
        pieceImage.sprite = confettiPixelSprite;
        pieceImage.color = ConfettiPalette[Random.Range(0, ConfettiPalette.Length)];

        float fallDistance = Random.Range(ConfettiFallDistanceMin, ConfettiFallDistanceMax);
        float fallDuration = Random.Range(ConfettiFallDurationMin, ConfettiFallDurationMax);
        float delay = Random.Range(0f, ConfettiSpawnStagger);
        float spinAngle = Random.Range(-540f, 540f);
        float driftX = Random.Range(-60f, 60f);
        Vector2 endPosition = startPosition + new Vector2(driftX, -fallDistance);

        pieceTransform.DOAnchorPos(endPosition, fallDuration).SetDelay(delay).SetEase(Ease.InQuad);
        pieceTransform.DORotate(new Vector3(0f, 0f, spinAngle), fallDuration, RotateMode.FastBeyond360).SetDelay(delay).SetEase(Ease.Linear);
        pieceImage.DOFade(0f, fallDuration * 0.35f).SetDelay(delay + fallDuration * 0.65f);

        Destroy(pieceObject, delay + fallDuration + 0.1f);
    }
}
