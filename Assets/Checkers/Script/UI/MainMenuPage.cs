using System.Collections.Generic;
using TMPro;
using UnityEngine;

// The main menu is the mode picker: a row of mode tabs, the selected mode's card, and the rules strip
// sit directly on this page, so a match is one tab and one PLAY away. PLAY starts the match with
// whatever ruleset the strip is showing and, for VsBot, whatever difficulty the card has selected.
// For the private-room mode PLAY is replaced by Create room and Join.
public class MainMenuPage : Page
{
    [Header("Modes")]
    [SerializeField] private ModeListSO modeList;

    // The mode cards and tabs are placed in the prefab, under these containers - one ModeTile and one
    // ModeTab per active mode, in the same order as the active entries of the mode list. Only the
    // selected mode's card is active, and the card around it resizes to fit. Nothing is created at
    // runtime; this page only fills them in (see SetupModes).
    [SerializeField] private Transform tileContainer;
    [SerializeField] private Transform tabContainer;

    [Header("Rules Strip")]
    [SerializeField] private RuleSetCard ruleSetCard;
    [SerializeField] private TappableCarousel ruleSetCarousel;
    [SerializeField] private TextMeshProUGUI rulesCaptionText;

    // PLAY for every mode except the private room, whose Create room / Join pair takes its place.
    [Header("Actions")]
    [SerializeField] private GameObject playSection;
    [SerializeField] private GameObject roomSection;

    [SerializeField] private GameDataSO gameDataSO;

    private readonly List<ModeInfo> activeModes = new List<ModeInfo>();
    private ModeTile[] tiles;
    private ModeTab[] tabs;
    private bool modesSetUp;
    private int selectedModeIndex;
    private int selectedRuleSetIndex;

    private void Awake()
    {
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    private void Start()
    {
        // MainMenuPage starts active in the scene rather than being opened via MenuPageManager, so
        // OnOpened() (which shows the shared top/bottom bars and fills in the tiles) would otherwise
        // never fire on first launch.
        if (ServiceLocator.Get<MenuPageManager>().CurrentActivePage == null)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.MainMenuPage);
        }
    }

    protected override void OnOpened()
    {
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuTopPanel);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuBottomPanel);

        SetupModes();
        SelectMode(selectedModeIndex);

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        int startIndex = settings.GetRuleSetIndex();
        ruleSetCarousel.Setup(settings.RuleSetCount, startIndex, OnRuleSetIndexChanged);
        OnRuleSetIndexChanged(startIndex);

        ListenForRoomLinks();
    }

    protected override void OnClosed()
    {
        StopListeningForRoomLinks();
    }

    private void OnDestroy()
    {
        StopListeningForRoomLinks();
    }

    // A room link can reach the game while this menu is open, or have launched it before the menu
    // existed (DeepLinkService keeps that one for a moment) - both end in the same place.
    private void ListenForRoomLinks()
    {
        if (!ServiceLocator.TryGet(out DeepLinkService links))
        {
            return;
        }

        links.RoomCodeReceived -= OpenJoinFromLink;
        links.RoomCodeReceived += OpenJoinFromLink;

        if (links.TryTakePendingCode(out string pendingCode))
        {
            OpenJoinFromLink(pendingCode);
        }
    }

    private void StopListeningForRoomLinks()
    {
        if (ServiceLocator.TryGet(out DeepLinkService links))
        {
            links.RoomCodeReceived -= OpenJoinFromLink;
        }
    }

    // Opens the Join popup with the link's code already in and joins straight away. If the popup is
    // already open the code is just put in the field, so it doesn't fight whatever is being typed.
    private void OpenJoinFromLink(string roomCode)
    {
        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        JoinRoomPage joinPage = pageManager.JoinRoomPage;

        if (pageManager.IsPageOpen(MenuPageType.JoinRoom))
        {
            joinPage.PrefillCode(roomCode);
            return;
        }

        ShowJoinPopup();
        joinPage.SubmitCode(roomCode);
    }

    // The device back button on the home screen asks before leaving the game - there is no deeper
    // page to go back to from here.
    public override bool OnBackPressed()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        DDOLPageManager ddolPageManager = ServiceLocator.Get<DDOLPageManager>();
        ddolPageManager.ConfirmationPopup.Show(
            "Quit game?",
            "Are you sure you want to quit the game?",
            "Yes",
            "No",
            QuitApplication,
            () => { });
        ddolPageManager.OpenPageAsOverlay(DDOLPageType.ConfirmationPopup);
        return true;
    }

    private static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Fills in the cards and tabs that are already in the prefab: the first of each gets the first
    // active mode, the second the second, and so on, in hierarchy order. A card or tab with no mode
    // left is hidden, and a mode with no card or tab is reported rather than silently missing from
    // the menu.
    private void SetupModes()
    {
        if (modesSetUp)
        {
            return;
        }

        tiles = tileContainer.GetComponentsInChildren<ModeTile>(true);
        tabs = tabContainer.GetComponentsInChildren<ModeTab>(true);
        int modeCount = 0;

        foreach (ModeInfo info in modeList.modes)
        {
            if (!info.isActive)
            {
                continue;
            }

            if (modeCount >= tiles.Length || modeCount >= tabs.Length)
            {
                Debug.LogError($"[MainMenuPage] No card or tab in the prefab for mode '{info.modeName}'. Add a ModeTile and a ModeTab for every active mode.");
                break;
            }

            int modeIndex = modeCount++;
            tiles[modeIndex].Setup(info);
            tabs[modeIndex].Setup(info, () => SelectMode(modeIndex));
            activeModes.Add(info);
        }

        for (int i = modeCount; i < tiles.Length; i++)
        {
            tiles[i].gameObject.SetActive(false);
        }

        for (int i = modeCount; i < tabs.Length; i++)
        {
            tabs[i].gameObject.SetActive(false);
        }

        modesSetUp = true;
    }

    // Shows the chosen mode's card, highlights its tab, and swaps PLAY for Create room / Join when it
    // is the private-room mode.
    private void SelectMode(int index)
    {
        if (activeModes.Count == 0)
        {
            return;
        }

        selectedModeIndex = Mathf.Clamp(index, 0, activeModes.Count - 1);

        for (int i = 0; i < activeModes.Count; i++)
        {
            tiles[i].gameObject.SetActive(i == selectedModeIndex);
            tabs[i].SetSelected(i == selectedModeIndex);
        }

        bool isPrivateRoom = activeModes[selectedModeIndex].mode == GameModeType.PrivateRoom;
        playSection.SetActive(!isPrivateRoom);
        roomSection.SetActive(isPrivateRoom);
        rulesCaptionText.text = isPrivateRoom ? "Rules for your room" : "Rules";
    }

    // Called on open and whenever the strip's arrows move - refreshes the name, the "1 / 9" counter
    // and the meta chips, and persists the choice, same as SettingPage does for its theme picker.
    private void OnRuleSetIndexChanged(int index)
    {
        selectedRuleSetIndex = index;

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        ruleSetCard.Setup(index, settings.GetRuleSet(index), settings.RuleSetCount);
        settings.SetRuleSetIndex(index);
    }

    public void OnPlayButtonClick()
    {
        if (activeModes.Count > 0)
        {
            OnModeSelected(activeModes[selectedModeIndex]);
        }
    }

    // PLAY doesn't start the match outright - it opens the confirm popup, which restates the mode
    // and the ruleset it is about to be played with. StartMatch runs only once that popup's Next is
    // pressed.
    private void OnModeSelected(ModeInfo info)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        RuleSetSO ruleSet = settings.GetRuleSet(selectedRuleSetIndex);

        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        pageManager.MatchConfirmPage.Show(info, ruleSet, BuildModeDetail(info, settings),
            () => StartMatch(info, ruleSet, settings));
        pageManager.OpenPageAsOverlay(MenuPageType.MatchConfirm);
    }

    // The sub-choice the tile itself carries, if it has one: the bot's difficulty. The other modes
    // have none, and the popup labels their opponent itself.
    private string BuildModeDetail(ModeInfo info, GameSettingsManager settings)
    {
        return info.mode == GameModeType.VsBot
            ? settings.GetBotDifficulty().ToString()
            : null;
    }

    private void StartMatch(ModeInfo info, RuleSetSO ruleSet, GameSettingsManager settings)
    {
        gameDataSO.ruleSet = ruleSet;
        gameDataSO.enableTurnTimer = info.enableTurnTimer;

        if (info.mode == GameModeType.VsBot)
        {
            gameDataSO.botDifficulty = settings.GetBotDifficulty();
        }

        ServiceLocator.Get<MatchmakingConnectionManager>().StartMatch(info.mode);
    }

    // Private rooms: the Create room and Join buttons that replace PLAY for the Friends mode. Create
    // goes through the same confirm popup as every other mode and then opens the matchmaking page
    // with the room code; Join opens a popup where the guest types the code.
    public void OnCreateRoomButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        ModeInfo info = FindMode(GameModeType.PrivateRoom);
        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        RuleSetSO ruleSet = settings.GetRuleSet(selectedRuleSetIndex);

        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        pageManager.MatchConfirmPage.Show(info, ruleSet, null, () => StartMatch(info, ruleSet, settings));
        pageManager.OpenPageAsOverlay(MenuPageType.MatchConfirm);
    }

    public void OnJoinRoomButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ShowJoinPopup();
    }

    private void ShowJoinPopup()
    {
        ModeInfo info = FindMode(GameModeType.PrivateRoom);

        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        JoinRoomPage joinPage = pageManager.JoinRoomPage;
        MatchmakingConnectionManager connection = ServiceLocator.Get<MatchmakingConnectionManager>();

        // The guest plays by the host's ruleset (adopted once the room is joined), but the turn timer
        // comes from the mode itself, same as for the host.
        joinPage.Show(
            code =>
            {
                gameDataSO.enableTurnTimer = info.enableTurnTimer;
                connection.JoinPrivateRoom(code, joinPage.OnJoinSucceeded, joinPage.ShowError);
            },
            connection.CancelPrivateRoomJoin);
        pageManager.OpenPageAsOverlay(MenuPageType.JoinRoom);
    }

    private ModeInfo FindMode(GameModeType mode)
    {
        foreach (ModeInfo info in modeList.modes)
        {
            if (info.mode == mode)
            {
                return info;
            }
        }

        return null;
    }
}
