public class GamePageManager : PageManager<GamePageManager, GamePageType>
{
    public GamePage GamePage => GetPage(GamePageType.GamePage) as GamePage;
    public RuleSetInfoPage RuleSetInfoPage => GetPage(GamePageType.RuleSetInfoPage) as RuleSetInfoPage;
    public VictoryPage VictoryPage => GetPage(GamePageType.VictoryPage) as VictoryPage;
    public DefeatPage DefeatPage => GetPage(GamePageType.DefeatPage) as DefeatPage;
    public DrawPage DrawPage => GetPage(GamePageType.DrawPage) as DrawPage;

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

    // A declined rematch disables the Rematch/Try Again/Play Again button on whichever one of these
    // three is actually showing for this client (each client only ever has one open, matching its own
    // outcome) - see GameManager.ReceiveRematchResponse. Checking IsPageOpen rather than assuming
    // which page applies keeps this correct regardless of which side (winner/loser/drawn) is calling.
    public void DisableRematchButton()
    {
        if (IsPageOpen(GamePageType.VictoryPage)) { VictoryPage.SetRematchButtonInteractable(false); }
        if (IsPageOpen(GamePageType.DefeatPage)) { DefeatPage.SetRematchButtonInteractable(false); }
        if (IsPageOpen(GamePageType.DrawPage)) { DrawPage.SetRematchButtonInteractable(false); }
    }
}
