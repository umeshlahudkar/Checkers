using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public static class UIUtils
{
    public static void Activate(this GameObject obj, float time = 0.20f)
    {
        obj.transform.localScale = Vector3.zero;
        obj.SetActive(true);
        obj.transform.DOScale(Vector3.one, time);
    }

    public static void Deactivate(this GameObject obj, float time = 0.10f)
    {
        obj.transform.DOScale(Vector3.zero, time).OnComplete( () => obj.SetActive(false));
    }

    // Resets a UI graphic to hidden (zero scale, zero alpha) then scales/fades it in - a punchy
    // "flourish" reveal reused across game-over pages (Victory/Defeat/Draw) so each result can pick
    // its own duration/delay/ease to feel distinct while sharing the same mechanic.
    public static void PopIn(this Graphic graphic, float duration, float delay = 0f, Ease ease = Ease.OutBack)
    {
        DOTween.Kill(graphic);
        DOTween.Kill(graphic.rectTransform);

        Color color = graphic.color;
        graphic.color = new Color(color.r, color.g, color.b, 0f);
        graphic.rectTransform.localScale = Vector3.zero;

        graphic.DOFade(1f, duration).SetDelay(delay);
        graphic.rectTransform.DOScale(1f, duration).SetDelay(delay).SetEase(ease);
    }

    // Slides a UI graphic in from an offset resting position while fading it in - the general form
    // behind DropIn (straight down) and also used directly for avatars entering from the left/right
    // and stat rows nudging in from the side. Completes any in-flight tween first so the captured
    // resting position can't drift if this is triggered again mid-animation.
    public static void SlideFadeIn(this Graphic graphic, Vector2 offset, float duration, float delay = 0f, Ease ease = Ease.OutQuad)
    {
        DOTween.Kill(graphic);
        DOTween.Kill(graphic.rectTransform, true);

        Color color = graphic.color;
        graphic.color = new Color(color.r, color.g, color.b, 0f);
        Vector2 restPosition = graphic.rectTransform.anchoredPosition;
        graphic.rectTransform.anchoredPosition = restPosition + offset;

        graphic.DOFade(1f, duration).SetDelay(delay);
        graphic.rectTransform.DOAnchorPos(restPosition, duration).SetDelay(delay).SetEase(ease);
    }

    // Like PopIn, but falls into its resting position from above instead of scaling up - used for a
    // heavier, "deflated" reveal rather than a bouncy pop.
    public static void DropIn(this Graphic graphic, float distance, float duration, float delay = 0f, Ease ease = Ease.InQuad)
    {
        graphic.SlideFadeIn(new Vector2(0f, distance), duration, delay, ease);
    }

    // Fades a UI graphic in with no movement or scale change - for elements that should simply appear
    // in place (e.g. a panel background) rather than pop or slide, since not everything needs motion.
    public static void FadeIn(this Graphic graphic, float duration, float delay = 0f)
    {
        DOTween.Kill(graphic);
        Color color = graphic.color;
        graphic.color = new Color(color.r, color.g, color.b, 0f);
        graphic.DOFade(1f, duration).SetDelay(delay);
    }

    // Slides a RectTransform up into its authored resting position - used for a content panel (e.g.
    // stats/buttons) rising into place after a "hero" flourish above it settles. Completes any
    // in-flight tween first so the captured resting position can't drift on repeated triggers.
    public static void SlideUpIn(this RectTransform rectTransform, float distance, float duration, float delay = 0f, Ease ease = Ease.OutQuad)
    {
        DOTween.Kill(rectTransform, true);

        Vector2 restPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = restPosition - new Vector2(0f, distance);
        rectTransform.DOAnchorPos(restPosition, duration).SetDelay(delay).SetEase(ease);
    }

    // Same slide-and-fade motion as SlideFadeIn, but for a CanvasGroup wrapping several children (e.g.
    // a stat row's label+value) instead of a single Graphic - so the whole group moves as one rigid
    // unit and fades together, rather than each child needing its own separately-tweened fade.
    public static void SlideFadeInGroup(this CanvasGroup group, Vector2 offset, float duration, float delay = 0f, Ease ease = Ease.OutQuad)
    {
        RectTransform rectTransform = (RectTransform)group.transform;
        DOTween.Kill(group);
        DOTween.Kill(rectTransform, true);

        group.alpha = 0f;
        Vector2 restPosition = rectTransform.anchoredPosition;
        rectTransform.anchoredPosition = restPosition + offset;

        group.DOFade(1f, duration).SetDelay(delay);
        rectTransform.DOAnchorPos(restPosition, duration).SetDelay(delay).SetEase(ease);
    }
}
