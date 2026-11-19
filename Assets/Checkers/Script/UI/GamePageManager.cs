public class GamePageManager : PageManager<GamePageManager, GamePageType>
{
    public GamePage GamePage => GetPage(GamePageType.GamePage) as GamePage;
    public RuleSetInfoPage RuleSetInfoPage => GetPage(GamePageType.RuleSetInfoPage) as RuleSetInfoPage;
    public ResultBannerPage ResultBannerPage => GetPage(GamePageType.ResultBanner) as ResultBannerPage;
    public ResultPopupPage ResultPopupPage => GetPage(GamePageType.ResultPopup) as ResultPopupPage;

    // True from the moment a result is handed in until its popup actually opens behind the banner.
    // A declined rematch can land inside that window, when no popup is open yet to disable.
    private bool isResultBannerPlaying;
    private bool rematchDeclinedDuringBanner;

    // The match is over and its result is on its way to the player (banner still playing) or on
    // screen (popup open). Used to keep a second end-of-match path from stacking another result.
    public bool IsResultShowing => isResultBannerPlaying || IsPageOpen(GamePageType.ResultPopup);

    // Single entry point for every match-end path (decisive win/loss, draw, forfeit) - GameManager
    // builds the GameResult, this fills in the popup, announces the outcome with the banner, and
    // opens the popup once the banner hands over.
    public void ShowGameResult(GameResult result)
    {
        ResultPopupPage.Show(result);

        isResultBannerPlaying = true;
        rematchDeclinedDuringBanner = false;

        ResultBannerPage.Show(result.Outcome, OpenResultPopup);
        OpenPageAsOverlay(GamePageType.ResultBanner);
    }

    private void OpenResultPopup()
    {
        isResultBannerPlaying = false;
        OpenPageAsOverlay(GamePageType.ResultPopup);

        if (rematchDeclinedDuringBanner)
        {
            rematchDeclinedDuringBanner = false;
            DisableRematchButton();
        }
    }

    // A declined rematch greys out the Rematch button on the popup - see
    // GameManager.ReceiveRematchResponse.
    //
    // A decline that arrives while the banner is still playing is remembered and applied the moment
    // the popup opens, since there is nothing to grey out yet.
    public void DisableRematchButton()
    {
        if (isResultBannerPlaying)
        {
            rematchDeclinedDuringBanner = true;
            return;
        }

        if (IsPageOpen(GamePageType.ResultPopup)) { ResultPopupPage.SetRematchDeclined(); }
    }

    // A rematch offer that timed out unanswered puts the button back to ready - see
    // GameManager.WaitForRematchOfferResponse.
    public void ResetRematchButton()
    {
        if (IsPageOpen(GamePageType.ResultPopup)) { ResultPopupPage.ResetRematchButton(); }
    }
}
