public class MenuPageManager : PageManager<MenuPageManager, MenuPageType>
{
    public RuleSetInfoPage RuleSetInfoPage => GetPage(MenuPageType.RuleSetInfoPage) as RuleSetInfoPage;
    public MatchConfirmPage MatchConfirmPage => GetPage(MenuPageType.MatchConfirm) as MatchConfirmPage;
    public JoinRoomPage JoinRoomPage => GetPage(MenuPageType.JoinRoom) as JoinRoomPage;
}
