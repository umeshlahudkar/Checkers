using DG.Tweening;
using TMPro;
using UnityEngine;

public class WarningNotifier : Service<WarningNotifier>
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform contentTransform;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private const float FadeInDuration = 0.3f;
    private const float HoldDuration = 2f;
    private const float FadeOutDuration = 0.4f;
    private const float FloatDistance = 40f;
    private const float PopInScale = 0.5f;

    private Sequence activeSequence;
    private Vector2 basePosition;

    protected override void Awake()
    {
        base.Awake();

        basePosition = contentTransform.anchoredPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // The GameObject must stay active in the saved scene so Awake (and ServiceLocator
        // registration) actually runs at load - Unity never calls Awake on an object that
        // starts inactive. Deactivating here instead of via the Inspector checkbox gives the
        // same "disabled until needed" result without breaking that registration.
        gameObject.SetActive(false);
    }

    // Pops out from half scale while fading in (no movement yet), holds still in place, then
    // moves upward while fading out.
    public void Show(string description)
    {
        gameObject.SetActive(true);
        descriptionText.text = description;

        // Kill (not Complete) any in-flight sequence so a spammed Show() restarts the animation
        // from the beginning instead of continuing/queuing, and so its OnComplete below can't
        // fire late and deactivate the object out from under the new sequence.
        activeSequence?.Kill();

        contentTransform.anchoredPosition = basePosition;
        contentTransform.localScale = Vector3.one * PopInScale;
        canvasGroup.alpha = 0f;

        activeSequence = DOTween.Sequence();
        activeSequence.Append(canvasGroup.DOFade(1f, FadeInDuration));
        activeSequence.Join(contentTransform.DOScale(1f, FadeInDuration).SetEase(Ease.OutBack));
        activeSequence.AppendInterval(HoldDuration);
        activeSequence.Append(canvasGroup.DOFade(0f, FadeOutDuration));
        activeSequence.Join(contentTransform.DOAnchorPosY(basePosition.y + FloatDistance, FadeOutDuration).SetEase(Ease.InCubic));
        activeSequence.OnComplete(() => gameObject.SetActive(false));
        activeSequence.SetTarget(this);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        activeSequence?.Kill();
    }

    [ContextMenu("Test Show Warning")]
    private void TestShow()
    {
        Show("Something went wrong. Please try again.");
    }
}
