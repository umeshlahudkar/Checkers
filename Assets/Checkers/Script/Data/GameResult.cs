public enum GameOutcome
{
    Victory,
    Defeat,
    Draw
}

// Single payload carrying everything the post-match popup (ResultPopupPage) needs -
// built once by GameManager for every match-end path (decisive win/loss, draw, forfeit) and handed
// to GamePageManager.ShowGameResult, which is the single entry point that plays the banner and
// opens the popup.
public class GameResult
{
    public GameOutcome Outcome;
    public string OpponentName;
    public int LocalPiecesLeft;
    public int OpponentPiecesLeft;
    public string Reason;
    public string MatchDuration;
}
