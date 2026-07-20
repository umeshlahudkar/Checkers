using DG.Tweening;
using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private RectTransform rectTransform;

    private const float PopInDuration = 0.2f;
    private const float HoldDuration = 0.5f;
    private const float FadeOutDuration = 0.4f;
    private const float FloatDistance = 60f;

    // Exposed so callers queuing multiple callouts (e.g. GamePage) know how long to wait before
    // showing the next one, instead of overlapping on the same spot.
    public const float TotalDuration = PopInDuration + HoldDuration + FadeOutDuration;

    // Pops in (fade + scale) at its spawn position, drifts upward the whole time it's visible,
    // holds, then fades out and destroys itself - a small "gratification" callout for events like
    // crowning a king or a multi-capture chain.
    public void Play(string text, Color color)
    {
        label.text = text;
        label.color = new Color(color.r, color.g, color.b, 0f);
        rectTransform.localScale = Vector3.one * 0.6f;

        float targetY = rectTransform.anchoredPosition.y + FloatDistance;

        Sequence sequence = DOTween.Sequence();
        sequence.Join(label.DOFade(1f, PopInDuration));
        sequence.Join(rectTransform.DOScale(1f, PopInDuration).SetEase(Ease.OutBack));
        sequence.Join(rectTransform.DOAnchorPosY(targetY, TotalDuration).SetEase(Ease.OutSine));
        sequence.Insert(PopInDuration + HoldDuration, label.DOFade(0f, FadeOutDuration));
        sequence.OnComplete(() => Destroy(gameObject));
    }
}
