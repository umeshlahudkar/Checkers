using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The popup that follows the result banner: one card for every outcome - the result line and its
// reason, how many pieces each side has left, the match time, and Rematch / Main Menu. Replaces the
// three full-screen Victory/Defeat/Draw pages, which only ever differed in colour and wording.
//
// Same Show(...)-then-OpenPageAsOverlay(...) shape as the other overlays: Show fills it in while it
// is still inactive, and OnOpened plays the entrance. The dim behind it fades in as the banner's own
// dim fades out, so the board never flashes lighter between the two.
public class ResultPopupPage : Page
{
    private enum RematchState
    {
        Ready,
        Waiting,
        Declined
    }

    [Header("Backdrop and card")]
    [SerializeField] private Image dimImage;
    [SerializeField] private RectTransform cardTransform;
    [SerializeField] private CanvasGroup cardGroup;
    [SerializeField] private Image accentLineImage;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI reasonText;
    [SerializeField] private TextMeshProUGUI localScoreText;
    [SerializeField] private TextMeshProUGUI localNameText;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI matchTimeText;

    // Result, reason, score row, match time and buttons - revealed one after another.
    [Header("Staggered rows")]
    [SerializeField] private CanvasGroup[] rowGroups;

    [Header("Rematch button")]
    [SerializeField] private Image rematchButtonImage;
    [SerializeField] private TextMeshProUGUI rematchLabelText;
    [SerializeField] private Sprite rematchReadySprite;
    [SerializeField] private Sprite rematchWaitingSprite;
    [SerializeField] private Sprite rematchDeclinedSprite;

    private const float DimFadeDuration = 0.3f;
    private const float CardDuration = 0.5f;
    private const float CardStartScale = 0.82f;
    private const float CardRiseDistance = 46f;

    private const float RowsDelay = 0.2f;
    private const float RowStagger = 0.1f;
    private const float RowDuration = 0.4f;
    private const float RowRiseDistance = 34f;

    private const float ScoreCountDelay = 0.4f;
    private const float ScoreCountDuration = 0.55f;

    private const float WaitingDotInterval = 0.4f;

    private static readonly Color VictoryTop = new Color(1f, 0.945f, 0.69f);
    private static readonly Color VictoryBottom = new Color(0.77f, 0.56f, 0.11f);
    private static readonly Color VictoryAccent = new Color(0.965f, 0.87f, 0.525f);
    private static readonly Color DefeatAccent = new Color(0.604f, 0.659f, 0.761f);
    private static readonly Color DrawAccent = new Color(0.898f, 0.918f, 0.953f);

    private static readonly Color BrightNumberColor = Color.white;
    private static readonly Color WinnerNameColor = new Color(0.89f, 0.72f, 0.23f);
    private static readonly Color MutedColor = new Color(0.56f, 0.61f, 0.71f);

    private static readonly Color ReadyLabelColor = new Color(0.23f, 0.153f, 0.02f);
    private static readonly Color WaitingLabelColor = new Color(0.86f, 0.89f, 0.95f);
    private static readonly Color DeclinedLabelColor = new Color(0.52f, 0.565f, 0.67f);

    // The dim is a partly-transparent overlay, so its rest alpha is cached once rather than assumed
    // to be 1 - same reason as ConfirmationPopup.bgRestAlpha.
    private float dimRestAlpha;
    private Vector2 cardRestPosition;

    private int localPieces;
    private int opponentPieces;
    private string rematchReadyText;
    private RematchState rematchState;
    private bool isLeaving;

    private Tween scoreCountTween;
    private Sequence entranceSequence;
    private Sequence waitingDotsSequence;

    private void Awake()
    {
        dimRestAlpha = dimImage.color.a;
        cardRestPosition = cardTransform.anchoredPosition;
    }

    public void Show(GameResult result)
    {
        Color accent;
        switch (result.Outcome)
        {
            case GameOutcome.Victory:
                resultText.text = "YOU WIN";
                resultText.color = Color.white;
                resultText.enableVertexGradient = true;
                resultText.colorGradient = new VertexGradient(VictoryTop, VictoryTop, VictoryBottom, VictoryBottom);
                accent = VictoryAccent;
                rematchReadyText = "REMATCH";
                break;
            case GameOutcome.Defeat:
                resultText.text = "YOU LOSE";
                resultText.enableVertexGradient = false;
                resultText.color = DefeatAccent;
                accent = DefeatAccent;
                rematchReadyText = "TRY AGAIN";
                break;
            default:
                resultText.text = "MATCH DRAWN";
                resultText.enableVertexGradient = false;
                resultText.color = DrawAccent;
                accent = DrawAccent;
                rematchReadyText = "PLAY AGAIN";
                break;
        }

        accentLineImage.color = accent;
        reasonText.text = FormatReason(result.Reason);

        // A drawn match leaves both sides bright; otherwise only the side that won stays bright.
        bool localBright = result.Outcome != GameOutcome.Defeat;
        bool opponentBright = result.Outcome != GameOutcome.Victory;

        localPieces = result.LocalPiecesLeft;
        opponentPieces = result.OpponentPiecesLeft;
        localScoreText.text = localPieces.ToString();
        opponentScoreText.text = opponentPieces.ToString();
        localScoreText.color = localBright ? BrightNumberColor : MutedColor;
        opponentScoreText.color = opponentBright ? BrightNumberColor : MutedColor;

        localNameText.text = "You";
        opponentNameText.text = result.OpponentName;
        localNameText.color = localBright ? WinnerNameColor : MutedColor;
        opponentNameText.color = opponentBright ? WinnerNameColor : MutedColor;

        matchTimeText.text = $"MATCH TIME  {result.MatchDuration}";

        isLeaving = false;
        SetRematchState(RematchState.Ready);
    }

    // The game hands reasons over in lower case mid-sentence ("no legal moves left").
    private static string FormatReason(string reason)
    {
        if (string.IsNullOrEmpty(reason)) { return string.Empty; }
        return char.ToUpperInvariant(reason[0]) + reason.Substring(1);
    }

    public void OnRematchButtonClick()
    {
        if (rematchState != RematchState.Ready) { return; }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager.GameMode == GameModeType.Multiplayer)
        {
            gameManager.OfferRematch();

            // OfferRematch silently does nothing when an offer is already out, so the button only
            // turns to "waiting" when there really is one to wait on.
            if (gameManager.RematchOfferPending)
            {
                SetRematchState(RematchState.Waiting);
            }
        }
        else
        {
            gameManager.StartRematch();
        }
    }

    public void OnMainMenuButtonClick()
    {
        if (isLeaving) { return; }
        isLeaving = true;

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        ServiceLocator.Get<GameManager>().GoToMainMenu();
    }

    // The match is over, so back simply leaves to the main menu, same as the Main Menu button.
    public override bool OnBackPressed()
    {
        OnMainMenuButtonClick();
        return true;
    }

    // The opponent said no (see GameManager.ReceiveRematchResponse).
    public void SetRematchDeclined()
    {
        SetRematchState(RematchState.Declined);
    }

    // The offer timed out unanswered (see GameManager.WaitForRematchOfferResponse) - the button goes
    // back to ready so another offer can be sent.
    public void ResetRematchButton()
    {
        if (rematchState == RematchState.Waiting)
        {
            SetRematchState(RematchState.Ready);
        }
    }

    // CustomButton (unlike a standard UI Button) has no built-in disabled state - gating the
    // button's own Image's raycast target is what actually stops IPointerDown/IPointerUp/OnClick
    // from ever reaching it (see CustomButton, and GamePage.SetOfferDrawButtonClickable for the same
    // trick applied to the Offer Draw button).
    private void SetRematchState(RematchState state)
    {
        rematchState = state;
        waitingDotsSequence?.Kill();
        waitingDotsSequence = null;

        switch (state)
        {
            case RematchState.Ready:
                rematchButtonImage.sprite = rematchReadySprite;
                rematchButtonImage.raycastTarget = true;
                rematchLabelText.color = ReadyLabelColor;
                rematchLabelText.text = rematchReadyText;
                break;
            case RematchState.Waiting:
                rematchButtonImage.sprite = rematchWaitingSprite;
                rematchButtonImage.raycastTarget = false;
                rematchLabelText.color = WaitingLabelColor;
                PlayWaitingDots();
                break;
            case RematchState.Declined:
                rematchButtonImage.sprite = rematchDeclinedSprite;
                rematchButtonImage.raycastTarget = false;
                rematchLabelText.color = DeclinedLabelColor;
                rematchLabelText.text = "REMATCH DECLINED";
                break;
        }
    }

    private void PlayWaitingDots()
    {
        int dots = 0;
        rematchLabelText.text = "WAITING";

        waitingDotsSequence = DOTween.Sequence().SetLink(gameObject);
        waitingDotsSequence.AppendInterval(WaitingDotInterval);
        waitingDotsSequence.AppendCallback(() =>
        {
            dots = (dots + 1) % 4;
            rematchLabelText.text = "WAITING" + new string('.', dots);
        });
        waitingDotsSequence.SetLoops(-1);
    }

    protected override void OnOpened()
    {
        // The card sizes itself through a VerticalLayoutGroup/ContentSizeFitter chain (a reason can
        // run to two lines), and the rows' rest positions are read off that layout below - so it has
        // to be built first. Has to happen here rather than in Show(): the page is still inactive
        // there, and ForceRebuildLayoutImmediate silently no-ops on anything inactive.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardTransform);

        PlayEntrance();
    }

    protected override void OnClosed()
    {
        entranceSequence?.Kill();
        scoreCountTween?.Kill();
        waitingDotsSequence?.Kill();
        entranceSequence = null;
        scoreCountTween = null;
        waitingDotsSequence = null;
    }

    private void PlayEntrance()
    {
        entranceSequence?.Kill();
        scoreCountTween?.Kill();

        // The banner's own dim is fading out right now, so this one fades in over the same stretch.
        Color dimColor = dimImage.color;
        dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);

        cardGroup.alpha = 0f;
        cardTransform.localScale = Vector3.one * CardStartScale;
        cardTransform.anchoredPosition = cardRestPosition + new Vector2(0f, -CardRiseDistance);

        entranceSequence = DOTween.Sequence().SetLink(gameObject);
        entranceSequence.Insert(0f, dimImage.DOFade(dimRestAlpha, DimFadeDuration));
        entranceSequence.Insert(0f, cardGroup.DOFade(1f, CardDuration * 0.6f));
        entranceSequence.Insert(0f, cardTransform.DOScale(1f, CardDuration).SetEase(Ease.OutBack));
        entranceSequence.Insert(0f, cardTransform.DOAnchorPos(cardRestPosition, CardDuration).SetEase(Ease.OutBack));

        for (int i = 0; i < rowGroups.Length; i++)
        {
            CanvasGroup row = rowGroups[i];
            RectTransform rowTransform = (RectTransform)row.transform;
            Vector2 rowRest = rowTransform.anchoredPosition;
            float delay = RowsDelay + i * RowStagger;

            row.alpha = 0f;
            rowTransform.anchoredPosition = rowRest + new Vector2(0f, -RowRiseDistance);

            entranceSequence.Insert(delay, row.DOFade(1f, RowDuration));
            entranceSequence.Insert(delay, rowTransform.DOAnchorPos(rowRest, RowDuration).SetEase(Ease.OutCubic));
        }

        localScoreText.text = "0";
        opponentScoreText.text = "0";
        scoreCountTween = DOVirtual.Float(0f, 1f, ScoreCountDuration, progress =>
        {
            localScoreText.text = Mathf.RoundToInt(localPieces * progress).ToString();
            opponentScoreText.text = Mathf.RoundToInt(opponentPieces * progress).ToString();
        }).SetDelay(ScoreCountDelay).SetEase(Ease.OutCubic).SetLink(gameObject);
    }
}
