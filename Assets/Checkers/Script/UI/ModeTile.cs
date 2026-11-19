using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The card for one mode on the main menu. The page keeps one card per mode and shows only the
// selected mode's. Every card has the same head - icon, name, description, online badge - and
// VsBot adds the difficulty buttons under it, which is why its card is taller. Starting the match
// is the page's PLAY button, not the card.
public class ModeTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject onlineBadge;

    // The card's height with and without the difficulty buttons. The page's card lays itself out
    // around whichever mode is shown, so this is all it needs to know.
    [Header("Height")]
    [SerializeField] private float headOnlyHeight;
    [SerializeField] private float withDifficultyHeight;

    // The bot difficulty, picked inline on the card. Defaults to whatever difficulty was last
    // picked.
    [Header("Bot Difficulty (VsBot only)")]
    [SerializeField] private GameObject difficultySection;

    [SerializeField] private TextMeshProUGUI easyLabel;
    [SerializeField] private Image easyImg;

    [SerializeField] private TextMeshProUGUI mediumLabel;
    [SerializeField] private Image mediumImg;

    [SerializeField] private TextMeshProUGUI hardLabel;
    [SerializeField] private Image hardImg;

    [SerializeField] private Color SelectedLabelColor;
    [SerializeField] private Color UnselectedLabelColor;

    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unSelectedSprite;

    public void Setup(ModeInfo info)
    {
        iconImage.sprite = info.modeIcon;
        nameText.text = info.modeName;
        descriptionText.text = info.modeDescription;
        onlineBadge.SetActive(info.showOnlineBadge);

        bool isVsBot = info.mode == GameModeType.VsBot;

        difficultySection.SetActive(isVsBot);

        RectTransform rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, isVsBot ? withDifficultyHeight : headOnlyHeight);

        if (isVsBot)
        {
            HighlightDifficulty(ServiceLocator.Get<GameSettingsManager>().GetBotDifficulty());
        }
    }

    public void OnEasyButtonClick()
    {
        SelectDifficulty(BotDifficulty.Easy);
    }

    public void OnMediumButtonClick()
    {
        SelectDifficulty(BotDifficulty.Medium);
    }

    public void OnHardButtonClick()
    {
        SelectDifficulty(BotDifficulty.Hard);
    }

    // Picking a difficulty only selects it - the confirm popup that PLAY opens shows it.
    private void SelectDifficulty(BotDifficulty difficulty)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GameSettingsManager>().SetBotDifficulty(difficulty);
        HighlightDifficulty(difficulty);
    }

    private void HighlightDifficulty(BotDifficulty difficulty)
    {
        bool easySelected = difficulty == BotDifficulty.Easy;
        bool mediumSelected = difficulty == BotDifficulty.Medium;
        bool hardSelected = difficulty == BotDifficulty.Hard;

        easyLabel.color = easySelected ? SelectedLabelColor : UnselectedLabelColor;
        mediumLabel.color = mediumSelected ? SelectedLabelColor : UnselectedLabelColor;
        hardLabel.color = hardSelected ? SelectedLabelColor : UnselectedLabelColor;

        easyImg.sprite = easySelected ? selectedSprite : unSelectedSprite;
        mediumImg.sprite = mediumSelected ? selectedSprite : unSelectedSprite;
        hardImg.sprite = hardSelected ? selectedSprite : unSelectedSprite;
    }
}
