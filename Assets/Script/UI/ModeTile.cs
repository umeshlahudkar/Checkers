using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    // Shown only on the VsBot tile - a sub-choice inline on the tile itself rather than a
    // click-to-reveal step, since tapping the tile's own button still starts the match immediately
    // (see ModeSelectionPage.OnModeSelected). Defaults to whatever difficulty was last picked.
    [Header("Bot Difficulty (VsBot tile only)")]
    [SerializeField] private GameObject difficultySection;
    [SerializeField] private GameObject easySelectedBorder;
    [SerializeField] private GameObject mediumSelectedBorder;
    [SerializeField] private GameObject hardSelectedBorder;

    private ModeInfo modeInfo;
    private Action<ModeInfo> onButtonClick;

    public void Setup(ModeInfo info, Action<ModeInfo> onSelected)
    {
        modeInfo = info;
        onButtonClick = onSelected;

        iconImage.sprite = info.modeIcon;
        nameText.text = info.modeName;
        descriptionText.text = info.modeDescription;

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
        easySelectedBorder.SetActive(difficulty == BotDifficulty.Easy);
        mediumSelectedBorder.SetActive(difficulty == BotDifficulty.Medium);
        hardSelectedBorder.SetActive(difficulty == BotDifficulty.Hard);
    }
}
