public class MenuPageManager : PageManager<MenuPageManager, MenuPageType>
{
    public RuleSetInfoPage RuleSetInfoPage => GetPage(MenuPageType.RuleSetInfoPage) as RuleSetInfoPage;
}
