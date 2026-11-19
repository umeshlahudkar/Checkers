public class PvpModeHandler : OfflineMatchModeHandlerBase
{
    public PvpModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.VsPlayer;

    protected override void SetupPlayerInfo()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        PieceType ownPieceType = (PieceType)profileManager.GetProfilePieceID();
        PieceType opponentPieceType = (ownPieceType == PieceType.White) ? PieceType.Black : PieceType.White;

        gameDataSO.ownPlayer = new PlayerInfo
        {
            userName = ownPieceType.ToString(),
            avatar = profileManager.GetPieceAvtar(ownPieceType),
            pieceType = ownPieceType
        };

        gameDataSO.opponentPlayer = new PlayerInfo
        {
            userName = opponentPieceType.ToString(),
            avatar = profileManager.GetPieceAvtar(opponentPieceType),
            pieceType = opponentPieceType
        };

        // Explicit reset, not just "default false" - gameDataSO is a persistent asset reused across
        // matches, so a previous VsBot (or disguised bot-fallback) match's true would otherwise leak
        // into this one.
        gameDataSO.opponentIsBot = false;
    }
}
