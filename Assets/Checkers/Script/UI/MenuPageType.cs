
public enum MenuPageType
{
    None = 0,
    MainMenuPage = 1,

    // 2 was ModeSelectionPage, retired when the main menu absorbed the mode grid and the rules
    // strip. The number is left unused rather than reclaimed: every member below keeps the value
    // Unity has already serialised for it - MenuPageManager's page list, MenuBottomPanel's tiles,
    // and the int arguments baked into those tiles' button events all store these as plain ints.

    ProfilePage = 3,

    UserNameInput = 4,
    AvtarSelection = 5,
    Setting = 6,
    Matchmaking = 7,
    RuleSetInfoPage = 8,

    MenuTopPanel = 9,
    MenuBottomPanel = 10,

    MatchConfirm = 11,
    JoinRoom = 12
}
