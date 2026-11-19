using System.Collections;
using UnityEngine;

public class FaderPage : Page
{
    public CanvasGroup canvasGroup;

    public IEnumerator FadeIn(float duration = 0.5f)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeOut(float duration = 0.5f)
    {
        yield return Fade(1f, 0f, duration);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
