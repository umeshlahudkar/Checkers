using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Spawned at a piece's board position when it's crowned - built deliberately differently from
// CaptureEffect (which never rotates and only bursts once) so a crowning doesn't read as another
// capture. Here a glow spins while it scales/fades, and a ring pulses outward twice underneath it,
// giving a "shine/flourish" feel rather than a "hit" flash. Destroys itself once done.
public class CrownEffect : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Image glowImage;
    [SerializeField] private Image ringImage;

    [SerializeField] private float glowDuration = 0.75f;
    [SerializeField] private float glowSpinAngle = 180f;
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private float ringScale = 1.6f;
    [SerializeField] private float startAlpha = 1f;
    [SerializeField] private float targetAlpha = 0f;

    public RectTransform ThisTransform => thisTransform;

    // [ContextMenu] lets this be re-triggered from the component's inspector context menu while
    // in Play mode, to preview the effect on a scene instance without needing a real promotion.
    [ContextMenu("Play Animation")]
    public void Play()
    {
        PlayGlow();
        PlayRingPulses();
        Destroy(gameObject, glowDuration);
    }

    private void PlayGlow()
    {
        if (glowImage == null)
        {
            return;
        }

        glowImage.transform.localScale = Vector3.one * 0.6f;
        glowImage.transform.localRotation = Quaternion.identity;
        glowImage.color = new Color(glowImage.color.r, glowImage.color.g, glowImage.color.b, startAlpha);

        glowImage.transform.DOScale(1.3f, glowDuration).SetEase(Ease.OutQuad);
        glowImage.transform.DORotate(new Vector3(0f, 0f, glowSpinAngle), glowDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);
        glowImage.DOFade(targetAlpha, glowDuration).SetEase(Ease.InQuad);
    }

    // Pulses outward twice rather than once, so it reads as a celebratory flourish instead of a
    // single capture-style hit flash - only fades out on the second pulse.
    private void PlayRingPulses()
    {
        if (ringImage == null)
        {
            return;
        }

        ringImage.transform.localScale = Vector3.zero;
        ringImage.color = new Color(ringImage.color.r, ringImage.color.g, ringImage.color.b, startAlpha);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(ringImage.transform.DOScale(ringScale, pulseDuration).SetEase(Ease.OutQuad));
        sequence.Append(ringImage.transform.DOScale(0f, pulseDuration).SetEase(Ease.InQuad));
        sequence.Append(ringImage.transform.DOScale(ringScale * 1.2f, pulseDuration).SetEase(Ease.OutQuad));
        sequence.Join(ringImage.DOFade(targetAlpha, pulseDuration).SetDelay(pulseDuration * 2f).SetEase(Ease.InQuad));
    }
}
