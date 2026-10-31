using UnityEngine;

// The main menu is now the mode picker: the grid of mode tiles and the rules strip that used to
// live on ModeSelectionPage sit directly on this page, so a match is one tap from launch instead of
// two. Tapping a tile starts the match with whatever ruleset the strip is showing and, for VsBot,
// whatever difficulty is selected inline on that tile.
public class MainMenuPage : Page
{
    [Header("Modes")]
    [SerializeField] private ModeListSO modeList;
    [SerializeField] private ModeTile tileTemplate;
    [SerializeField] private Transform tileContainer;

    [Header("Rules Strip")]
    [SerializeField] private RuleSetCard ruleSetCard;
    [SerializeField] private TappableCarousel ruleSetCarousel;

    [SerializeField] private GameDataSO gameDataSO;

    private bool tilesCreated;
    private int selectedRuleSetIndex;

    private void Awake()
    {
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    private void Start()
    {
        // MainMenuPage starts active in the scene rather than being opened via MenuPageManager, so
        // OnOpened() (which shows the shared top/bottom bars and builds the grid) would otherwise
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

        CreateTiles();

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

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        foreach (ModeInfo info in modeList.modes)
        {
            if (!info.isActive)
            {
                continue;
            }

            ModeTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(info, OnModeSelected, OnCreateRoom, OnJoinRoom);
        }

        tilesCreated = true;
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

    // Tapping a tile no longer starts the match outright - it opens the confirm popup, which
    // restates the mode and the ruleset it is about to be played with. StartMatch runs only once
    // that popup's Next is pressed.
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

    // Private rooms: the Friends tile's Create and Join buttons. Create goes through the same confirm
    // popup as every other mode and then opens the matchmaking page with the room code; Join opens a
    // popup where the guest types the code.
    private void OnCreateRoom()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        ModeInfo info = FindMode(GameModeType.PrivateRoom);
        GameSettingsManager settings = ServiceLocator.Get<GameSettingsManager>();
        RuleSetSO ruleSet = settings.GetRuleSet(selectedRuleSetIndex);

        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        pageManager.MatchConfirmPage.Show(info, ruleSet, null, () => StartMatch(info, ruleSet, settings));
        pageManager.OpenPageAsOverlay(MenuPageType.MatchConfirm);
    }

    private void OnJoinRoom()
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
