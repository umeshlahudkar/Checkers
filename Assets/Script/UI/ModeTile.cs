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

    // Selected / unselected look, shared by the difficulty buttons and the Create / Join pair.
    [SerializeField] private Color SelectedLabelColor;
    [SerializeField] private Color UnselectedLabelColor;

    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unSelectedSprite;

    // Footer variant 3 - Create and Join sit on the tile for the same reason the difficulty buttons
    // do: the branch is the choice, so putting it here saves the player a whole screen.
    [Header("Footer - Private Room (PrivateRoom only)")]
    [SerializeField] private GameObject roomSection;

    [SerializeField] private TextMeshProUGUI createLabel;
    [SerializeField] private Image createImg;

    [SerializeField] private TextMeshProUGUI joinLabel;
    [SerializeField] private Image joinImg;

    private ModeInfo modeInfo;
    private Action<ModeInfo> onButtonClick;
    private Action onCreateRoomClick;
    private Action onJoinRoomClick;

    // Which of Create / Join a tap on the tile body goes with - Create until the player picks Join.
    private bool joinSelected;

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
        else if (isPrivateRoom)
        {
            HighlightRoomAction(joinSelected);
        }
        else
        {
            footerLabelText.text = info.footerLabel;
        }
    }

    // A tap anywhere on the tile moves on with the tile's current choice: the selected difficulty for
    // VsBot, the selected Create / Join for the private-room tile, nothing extra for the rest.
    public void OnButtonClick()
    {
        if (modeInfo == null) { return; }

        if (modeInfo.mode == GameModeType.PrivateRoom)
        {
            (joinSelected ? onJoinRoomClick : onCreateRoomClick)?.Invoke();
            return;
        }

        onButtonClick?.Invoke(modeInfo);
    }

    public void OnCreateRoomButtonClick()
    {
        joinSelected = false;
        HighlightRoomAction(false);
        onCreateRoomClick?.Invoke();
    }

    public void OnJoinRoomButtonClick()
    {
        joinSelected = true;
        HighlightRoomAction(true);
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

    // Picking a difficulty also moves on, same as tapping the tile - the confirm popup then shows
    // the difficulty that was just picked.
    private void SelectDifficulty(BotDifficulty difficulty)
    {
        ServiceLocator.Get<GameSettingsManager>().SetBotDifficulty(difficulty);
        HighlightDifficulty(difficulty);
        onButtonClick?.Invoke(modeInfo);
    }

    private void HighlightRoomAction(bool joinIsSelected)
    {
        createLabel.color = joinIsSelected ? UnselectedLabelColor : SelectedLabelColor;
        joinLabel.color = joinIsSelected ? SelectedLabelColor : UnselectedLabelColor;

        createImg.sprite = joinIsSelected ? unSelectedSprite : selectedSprite;
        joinImg.sprite = joinIsSelected ? selectedSprite : unSelectedSprite;
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
