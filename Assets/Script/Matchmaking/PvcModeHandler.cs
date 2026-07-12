public class PvcModeHandler : OfflineMatchModeHandlerBase
{
    public PvcModeHandler(PhotonNetworkManager photonNetworkManager, GameDataSO gameDataSO)
        : base(photonNetworkManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.VsBot;
}
