public class OfflineMatchModeHandlerBase : MatchModeHandler
{
    public override GameModeType Mode => GameModeType.VsPlayer;

    protected OfflineMatchModeHandlerBase(PhotonNetworkManager photonNetworkManager, GameDataSO gameDataSO)
        : base(photonNetworkManager, gameDataSO)
    {
    }

    public override void StartMatch()
    {
        gameDataSO.gameMode = Mode;
        photonNetworkManager.StartOfflineMatch();
    }

    public override void OnJoinedRoom()
    {
        ServiceLocator.Get<SceneLoader>().LoadScene("GameplayScene");
    }
}