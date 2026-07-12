using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingPage : Page
{
    [SerializeField] private Slider progressBarFill;
    [SerializeField] private TextMeshProUGUI loadingText;

    public void SetProgress(float progress01, string message = null)
    {
        if (progressBarFill != null)
        {
            progressBarFill.value = Mathf.Clamp01(progress01);
        }

        if (!string.IsNullOrEmpty(message) && loadingText != null)
        {
            loadingText.text = message;
        }
    }

    protected override void OnOpened()
    {
        SetProgress(0f);
    }
}
