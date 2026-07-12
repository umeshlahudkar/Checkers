public class PvpModeHandler : OfflineMatchModeHandlerBase
{
    public PvpModeHandler(PhotonNetworkManager photonNetworkManager, GameDataSO gameDataSO)
        : base(photonNetworkManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.VsPlayer;
}
