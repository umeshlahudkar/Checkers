using UnityEngine;

public enum GameOutcome
{
    Victory,
    Defeat,
    Draw
}

// Single payload carrying everything the post-match pages (VictoryPage/DefeatPage/DrawPage) need -
// built once by GameManager for every match-end path (decisive win/loss, draw, forfeit) and handed
// to GamePageManager.ShowGameResult, which is the single entry point that opens the right page.
public class GameResult
{
    public GameOutcome Outcome;
    public string OpponentName;
    public Sprite OpponentAvatar;
    public int LocalPiecesLeft;
    public int OpponentPiecesLeft;
    public string Reason;
    public int LocalCaptures;
    public int OpponentCaptures;
    public int LocalKingsCrowned;
    public int OpponentKingsCrowned;
    public int LocalLongestChain;
    public int OpponentLongestChain;
    public string MatchDuration;
}
