using Photon.Realtime;

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
        SetupPlayerInfo();

        if(photonNetworkManager.IsConnected)
        {
            photonNetworkManager.Disconnect();
        }
        else
        {
            StartGameplayScene();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        StartGameplayScene();
    }

    protected virtual void SetupPlayerInfo()
    {
    }

    private void StartGameplayScene()
    {
        ServiceLocator.Get<SceneLoader>().LoadScene("GameplayScene");
    }
}
