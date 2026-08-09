public class GamePageManager : PageManager<GamePageManager, GamePageType>
{
    public GamePage GamePage => GetPage(GamePageType.GamePage) as GamePage;
    public RuleSetInfoPage RuleSetInfoPage => GetPage(GamePageType.RuleSetInfoPage) as RuleSetInfoPage;
    public VictoryPage VictoryPage => GetPage(GamePageType.VictoryPage) as VictoryPage;
    public DefeatPage DefeatPage => GetPage(GamePageType.DefeatPage) as DefeatPage;
    public DrawPage DrawPage => GetPage(GamePageType.DrawPage) as DrawPage;
    public DrawOfferPage DrawOfferPage => GetPage(GamePageType.DrawOfferPage) as DrawOfferPage;

    // Single entry point for every match-end path (decisive win/loss, draw, forfeit) - GameManager
    // builds the GameResult, this picks and populates the matching page.
    public void ShowGameResult(GameResult result)
    {
        switch (result.Outcome)
        {
            case GameOutcome.Victory:
                VictoryPage.Show(result);
                OpenPageAsOverlay(GamePageType.VictoryPage);
                break;
            case GameOutcome.Defeat:
                DefeatPage.Show(result);
                OpenPageAsOverlay(GamePageType.DefeatPage);
                break;
            case GameOutcome.Draw:
                DrawPage.Show(result);
                OpenPageAsOverlay(GamePageType.DrawPage);
                break;
        }
    }
}
