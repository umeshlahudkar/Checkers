using Photon.Pun;
using Photon.Realtime;

public class OfflineMatchModeHandlerBase : MatchModeHandler
{
    public override GameModeType Mode => GameModeType.VsPlayer;

    protected OfflineMatchModeHandlerBase(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
        : base(connectionManager, gameDataSO)
    {
    }

    public override void StartMatch()
    {
        gameDataSO.gameMode = Mode;
        SetupPlayerInfo();

        if(connectionManager.IsConnected)
        {
            connectionManager.Disconnect();
        }
        else
        {
            EnterOfflineRoom();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        EnterOfflineRoom();
    }

    // GameplayScene now always relies on PhotonView/RPC (see GameManager.SpawnLocalPlayer), even for
    // local matches, so it needs a real (offline) room to spawn into before it loads. CreateRoom's
    // OnCreatedRoom/OnJoinedRoom fire synchronously in OfflineMode, so StartGameplayScene runs
    // immediately via OnJoinedRoom below.
    private void EnterOfflineRoom()
    {
        PhotonNetwork.OfflineMode = true;
        connectionManager.CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        StartGameplayScene();
    }

    protected virtual void SetupPlayerInfo()
    {
    }

    private void StartGameplayScene()
    {
        ServiceLocator.Get<SceneLoader>().LoadScene(GameConstants.Scenes.GameplayScene);
    }
}
