using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One square tile in the main menu's 2x2 mode grid. Every tile shares the same head - icon, name,
// description, online badge - and differs only in its footer strip, which is one of three things
// depending on the mode: the bot difficulty buttons, a plain label, or the private-room Create/Join
// pair. Exactly one of those three is active at a time, chosen in Setup.
public class ModeTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject onlineBadge;

    // Footer variant 1 - a static line of detail (VsPlayer, Multiplayer). Text comes from
    // ModeInfo.footerLabel.
    [Header("Footer - Label")]
    [SerializeField] private GameObject labelFooter;
    [SerializeField] private TextMeshProUGUI footerLabelText;

    // Footer variant 2 - a sub-choice inline on the tile itself rather than a click-to-reveal step,
    // since tapping the tile's own button still starts the match immediately (see
    // MainMenuPage.OnModeSelected). Defaults to whatever difficulty was last picked.
    [Header("Footer - Bot Difficulty (VsBot only)")]
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

    // Footer variant 3 - Create and Join sit on the tile for the same reason the difficulty buttons
    // do: the branch is the choice, so putting it here saves the player a whole screen.
    [Header("Footer - Private Room (PrivateRoom only)")]
    [SerializeField] private GameObject roomSection;

    private ModeInfo modeInfo;
    private Action<ModeInfo> onButtonClick;
    private Action onCreateRoomClick;
    private Action onJoinRoomClick;

    public void Setup(ModeInfo info, Action<ModeInfo> onSelected, Action onCreateRoom, Action onJoinRoom)
    {
        modeInfo = info;
        onButtonClick = onSelected;
        onCreateRoomClick = onCreateRoom;
        onJoinRoomClick = onJoinRoom;

        iconImage.sprite = info.modeIcon;
        nameText.text = info.modeName;
        descriptionText.text = info.modeDescription;
        onlineBadge.SetActive(info.showOnlineBadge);

        bool isVsBot = info.mode == GameModeType.VsBot;
        bool isPrivateRoom = info.mode == GameModeType.PrivateRoom;

        difficultySection.SetActive(isVsBot);
        roomSection.SetActive(isPrivateRoom);
        labelFooter.SetActive(!isVsBot && !isPrivateRoom);

        if (isVsBot)
        {
            HighlightDifficulty(ServiceLocator.Get<GameSettingsManager>().GetBotDifficulty());
        }
        else if (!isPrivateRoom)
        {
            footerLabelText.text = info.footerLabel;
        }
    }

    public void OnButtonClick()
    {
        // The private-room tile has no whole-tile action - Create and Join in its footer are the two
        // things it can do - so a tap on the body itself is deliberately inert.
        if (modeInfo == null || modeInfo.mode == GameModeType.PrivateRoom) { return; }

        onButtonClick?.Invoke(modeInfo);
    }

    public void OnCreateRoomButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        onCreateRoomClick?.Invoke();
    }

    public void OnJoinRoomButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        onJoinRoomClick?.Invoke();
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
