using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SplashPage : Page
{
    [SerializeField] private Slider progressBarFill;
    [SerializeField] private TextMeshProUGUI loadingText;

    public void SetProgress(float progress01, string message = null)
    {
        if (!string.IsNullOrEmpty(message) && loadingText != null)
        {
            loadingText.text = message;
        }
    }

    private void Update()
    {
        
    }

    protected override void OnOpened()
    {
        SetProgress(0f);
    }
}
