using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Spawned at a captured piece's board position and left to play out on its own - unlike a child of
// the piece, it isn't affected by (and doesn't need cleanup tied to) the piece's own shrink-and-
// destroy tween, since the piece GameObject can disappear mid-effect.
public class CaptureEffect : MonoBehaviour
{
    [SerializeField] private RectTransform thisTransform;
    [SerializeField] private Image innerImage;
    [SerializeField] private Image outerImage;

    [SerializeField] private float innerDuration = 0.25f;
    [SerializeField] private float outerDuration = 0.35f;
    [SerializeField] private float outerScale = 1.6f;
    [SerializeField] private float startAlpha = 1f;
    [SerializeField] private float targetAlpha = 0f;

    public RectTransform ThisTransform => thisTransform;

    // Two overlapping white images burst at the captured square: a small inner flash that pops and
    // fades quickly, plus a bigger outer ring/glow that scales further out and lingers slightly
    // longer, giving the burst some depth instead of one flat flash. Destroys itself once the
    // slower (outer) layer finishes.
    //
    // [ContextMenu] lets this be re-triggered from the component's inspector context menu while
    // in Play mode, to preview the burst on a scene instance without needing a real capture.
    [ContextMenu("Play Animation")]
    public void Play()
    {
        PlayLayer(innerImage, 1f, innerDuration);
        PlayLayer(outerImage, outerScale, outerDuration);
        Destroy(gameObject, outerDuration);
    }

    private void PlayLayer(Image image, float targetScale, float duration)
    {
        if (image == null)
        {
            return;
        }

        image.transform.localScale = Vector3.zero;
        image.color = new Color(image.color.r, image.color.g, image.color.b, startAlpha);

        image.transform.DOScale(targetScale, duration).SetEase(Ease.OutQuad);
        image.DOFade(targetAlpha, duration).SetEase(Ease.InQuad);
    }
}
