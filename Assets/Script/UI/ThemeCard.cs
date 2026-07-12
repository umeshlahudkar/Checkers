using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ThemeCard : MonoBehaviour
{
    [SerializeField] private Image lightSwatchImage;
    [SerializeField] private Image darkSwatchImage;
    [SerializeField] private Image accentDotImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject selectedBorder;

    private int themeIndex;
    private Action<int> onSelected;

    public void Setup(int index, BoardThemeInfo info, Action<int> onThemeSelected)
    {
        themeIndex = index;
        onSelected = onThemeSelected;

        lightSwatchImage.color = info.lightSquareColor;
        darkSwatchImage.color = info.darkSquareColor;
        accentDotImage.color = info.accentColor;
        nameText.text = info.themeName;
    }

    public void OnCardClick()
    {
        onSelected?.Invoke(themeIndex);
    }

    public void SetSelected(bool selected)
    {
        selectedBorder.SetActive(selected);
    }
}
