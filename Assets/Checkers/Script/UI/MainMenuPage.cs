using UnityEngine;

// The main menu is a short list of modes: the gold Play Online card and one card each for Vs Bot,
// Pass and Play and Friends. Tapping a card opens the match setup popup, which is where the ruleset
// (and the bot's difficulty) is chosen and the match is started.
public class MainMenuPage : Page
{
    [Header("Modes")]
    [SerializeField] private ModeListSO modeList;

    // The cards are placed in the prefab and each one knows which mode it is for (see ModeCard). A
    // card whose mode is missing or switched off in the mode list is hidden.
    [SerializeField] private ModeCard[] modeCards;

    [SerializeField] private GameDataSO gameDataSO;

    private bool modesSetUp;

    private void Awake()
    {
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    private void Start()
    {
        // MainMenuPage starts active in the scene rather than being opened via MenuPageManager, so
        // OnOpened() (which shows the shared top/bottom bars and fills in the cards) would otherwise
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

        SetupModeCards();
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

    // Fills in the cards that are already in the prefab, each from its own mode's entry in the mode
    // list. Done once: the cards keep what they were given between visits to this page.
    private void SetupModeCards()
    {
        if (modesSetUp)
        {
            return;
        }

        foreach (ModeCard card in modeCards)
        {
            ModeInfo info = FindMode(card.Mode);

            if (info == null || !info.isActive)
            {
                card.gameObject.SetActive(false);
                continue;
            }

            card.Setup(info, OnModeSelected);
        }

        modesSetUp = true;
    }

    // A card doesn't start the match outright - it opens the setup popup, where the ruleset (and the
    // bot's difficulty) is picked. StartMatch runs only once that popup's Play or Create room is
    // pressed.
    private void OnModeSelected(ModeInfo info)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        MenuPageManager pageManager = ServiceLocator.Get<MenuPageManager>();
        pageManager.MatchConfirmPage.Show(info, ruleSet => StartMatch(info, ruleSet), ShowJoinPopup);
        pageManager.OpenPageAsOverlay(MenuPageType.MatchConfirm);
    }

    private void StartMatch(ModeInfo info, RuleSetSO ruleSet)
    {
        gameDataSO.ruleSet = ruleSet;
        gameDataSO.enableTurnTimer = info.enableTurnTimer;

        if (info.mode == GameModeType.VsBot)
        {
            gameDataSO.botDifficulty = ServiceLocator.Get<GameSettingsManager>().GetBotDifficulty();
        }

        ServiceLocator.Get<MatchmakingConnectionManager>().StartMatch(info.mode);
    }

    // Private rooms: the setup popup's Join button lands here. The guest types the room code in a
    // popup of its own.
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
