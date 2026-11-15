using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One tab in the main menu's mode row. Shows the mode's icon and name; the selected tab is drawn
// gold, the rest as dark cards. MainMenuPage decides which one is selected.
public class ModeTab : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI label;

    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unselectedColor;

    private Action onClick;

    public void Setup(ModeInfo info, Action onClick)
    {
        this.onClick = onClick;
        iconImage.sprite = info.modeIcon;
        label.text = info.modeName;
    }

    public void SetSelected(bool selected)
    {
        background.sprite = selected ? selectedSprite : unselectedSprite;
        iconImage.color = selected ? selectedColor : unselectedColor;
        label.color = selected ? selectedColor : unselectedColor;
    }

    public void OnTabButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        onClick?.Invoke();
    }
}
