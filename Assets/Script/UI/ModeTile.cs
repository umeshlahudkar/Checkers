using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject onlineBadge;

    // Shown only on the VsBot tile - a sub-choice inline on the tile itself rather than a
    // click-to-reveal step, since tapping the tile's own button still starts the match immediately
    // (see ModeSelectionPage.OnModeSelected). Defaults to whatever difficulty was last picked.
    [Header("Bot Difficulty (VsBot tile only)")]
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

    private ModeInfo modeInfo;
    private Action<ModeInfo> onButtonClick;

    public void Setup(ModeInfo info, Action<ModeInfo> onSelected)
    {
        modeInfo = info;
        onButtonClick = onSelected;

        iconImage.sprite = info.modeIcon;
        nameText.text = info.modeName;
        descriptionText.text = info.modeDescription;
        onlineBadge.SetActive(info.showOnlineBadge);

        bool isVsBot = info.mode == GameModeType.VsBot;
        difficultySection.SetActive(isVsBot);

        if (isVsBot)
        {
            HighlightDifficulty(ServiceLocator.Get<GameSettingsManager>().GetBotDifficulty());
        }
    }

    public void OnButtonClick()
    {
        onButtonClick?.Invoke(modeInfo);
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
