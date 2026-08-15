using UnityEngine;

public class PvcModeHandler : OfflineMatchModeHandlerBase
{
    public PvcModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.VsBot;

    protected override void SetupPlayerInfo()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        PieceType ownPieceType = (PieceType)Random.Range(1, 3);
        PieceType opponentPieceType = (ownPieceType == PieceType.White) ? PieceType.Black : PieceType.White;

        gameDataSO.ownPlayer = new PlayerInfo
        {
            userName = profileManager.GetUserName(),
            avatar = profileManager.GetProfileAvtar(),
            pieceType = ownPieceType
        };

        gameDataSO.opponentPlayer = new PlayerInfo
        {
            userName = "Computer",
            avatar = profileManager.GetComputerAvtar(),
            pieceType = opponentPieceType
        };

        gameDataSO.opponentIsBot = true;
    }
}
