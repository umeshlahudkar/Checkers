using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The first thing shown when a match ends: a dark band opens across the middle of the board and the
// outcome word - VICTORY, DEFEAT or DRAW - lands in it. Once it has held for a moment the result
// popup (ResultPopupPage) opens and this fades away behind it.
//
// Same Show(...)-then-OpenPageAsOverlay(...) shape as the other overlays: Show only stores what to
// play, and OnOpened starts it, since tweens need the page to be active. A tap anywhere skips ahead
// to the result page. The dim layer blocks the board and buttons underneath for the whole time, so
// nothing can be played (or undone) while the result is being announced.
public class ResultBannerPage : Page
{
    [SerializeField] private Image dimImage;
    [SerializeField] private RectTransform bandTransform;
    [SerializeField] private CanvasGroup bandGroup;
    [SerializeField] private Image topLineImage;
    [SerializeField] private Image bottomLineImage;
    [SerializeField] private RectTransform titleTransform;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private TextMeshProUGUI titleText;

    private const float DimFadeDuration = 0.22f;
    private const float BandOpenDuration = 0.31f;
    private const float TitleDelay = 0.22f;
    private const float TitleFadeDuration = 0.3f;
    private const float TitlePopDuration = 0.4f;
    private const float TitleStartScale = 1.7f;

    // Total time from the first frame until the result page is asked to open.
    private const float HoldUntil = 1.9f;
    private const float ExitFadeDuration = 0.3f;

    private static readonly Color VictoryTop = new Color(1f, 0.945f, 0.69f);
    private static readonly Color VictoryBottom = new Color(0.77f, 0.56f, 0.11f);
    private static readonly Color VictoryLine = new Color(0.965f, 0.87f, 0.525f);
    private static readonly Color DefeatColor = new Color(0.604f, 0.659f, 0.761f);
    private static readonly Color DrawColor = new Color(0.898f, 0.918f, 0.953f);

    // The dim is a partly-transparent overlay, so its rest alpha is cached once rather than assumed
    // to be 1 - same reason as ConfirmationPopup.bgRestAlpha.
    private float dimRestAlpha;

    private GameOutcome outcome;
    private Action onFinished;
    private Sequence sequence;

    // A tap can land on the same frame the sequence completes by itself, and the result page must
    // only ever be opened once. Reset on every open.
    private bool isFinished;

    private void Awake()
    {
        dimRestAlpha = dimImage.color.a;
    }

    public void Show(GameOutcome result, Action onFinish)
    {
        outcome = result;
        onFinished = onFinish;
    }

    public void OnSkipButtonClick()
    {
        Finish();
    }

    // Swallowed: the announcement is a couple of seconds long, and a press that lands inside it
    // shouldn't be able to reach anything underneath (or leave the match before the result shows).
    public override bool OnBackPressed()
    {
        return true;
    }

    protected override void OnOpened()
    {
        isFinished = false;
        ApplyOutcomeStyle();
        PlayEntrance();
    }

    protected override void OnClosed()
    {
        sequence?.Kill();
        sequence = null;
    }

    private void ApplyOutcomeStyle()
    {
        Color lineColor;
        switch (outcome)
        {
            case GameOutcome.Victory:
                titleText.text = "VICTORY";
                titleText.color = Color.white;
                titleText.enableVertexGradient = true;
                titleText.colorGradient = new VertexGradient(VictoryTop, VictoryTop, VictoryBottom, VictoryBottom);
                lineColor = VictoryLine;
                break;
            case GameOutcome.Defeat:
                titleText.text = "DEFEAT";
                titleText.enableVertexGradient = false;
                titleText.color = DefeatColor;
                lineColor = DefeatColor;
                break;
            default:
                titleText.text = "DRAW";
                titleText.enableVertexGradient = false;
                titleText.color = DrawColor;
                lineColor = DrawColor;
                break;
        }

        topLineImage.color = lineColor;
        bottomLineImage.color = lineColor;
    }

    private void PlayEntrance()
    {
        sequence?.Kill();

        Color dimColor = dimImage.color;
        dimImage.color = new Color(dimColor.r, dimColor.g, dimColor.b, 0f);

        bandGroup.alpha = 1f;
        bandTransform.localScale = new Vector3(0f, 1f, 1f);

        titleGroup.alpha = 0f;
        titleTransform.localScale = Vector3.one * TitleStartScale;

        sequence = DOTween.Sequence().SetLink(gameObject);
        sequence.Insert(0f, dimImage.DOFade(dimRestAlpha, DimFadeDuration));
        sequence.Insert(0f, bandTransform.DOScaleX(1f, BandOpenDuration).SetEase(Ease.OutCubic));
        sequence.Insert(TitleDelay, titleGroup.DOFade(1f, TitleFadeDuration));
        sequence.Insert(TitleDelay, titleTransform.DOScale(1f, TitlePopDuration).SetEase(Ease.OutBack));
        sequence.InsertCallback(HoldUntil, Finish);
    }

    // Hands over to the result popup, then fades out underneath it. The popup sits above this one in
    // the canvas, so it simply opens over the fading banner.
    private void Finish()
    {
        if (isFinished) { return; }
        isFinished = true;

        sequence?.Kill();

        onFinished?.Invoke();

        sequence = DOTween.Sequence().SetLink(gameObject);
        sequence.Insert(0f, dimImage.DOFade(0f, ExitFadeDuration));
        sequence.Insert(0f, bandGroup.DOFade(0f, ExitFadeDuration));
        sequence.Insert(0f, titleGroup.DOFade(0f, ExitFadeDuration));
        sequence.OnComplete(() => ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.ResultBanner));
    }
}
