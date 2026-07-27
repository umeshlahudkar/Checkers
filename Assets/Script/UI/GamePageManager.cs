public class GamePageManager : PageManager<GamePageManager, GamePageType>
{
    public GamePage GamePage => GetPage(GamePageType.GamePage) as GamePage;
    public ResultPage ResultPage => GetPage(GamePageType.ResultPage) as ResultPage;
    public RuleSetInfoPage RuleSetInfoPage => GetPage(GamePageType.RuleSetInfoPage) as RuleSetInfoPage;
}
