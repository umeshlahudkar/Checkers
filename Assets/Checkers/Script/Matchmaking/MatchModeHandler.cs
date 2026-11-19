using System.Collections.Generic;
using Photon.Realtime;

public abstract class MatchModeHandler
{
    protected readonly MatchmakingConnectionManager connectionManager;
    protected readonly GameDataSO gameDataSO;
    protected MatchmakingPage matchmakingPage;

    protected MatchModeHandler(MatchmakingConnectionManager connectionManager, GameDataSO gameDataSO)
    {
        this.connectionManager = connectionManager;
        this.gameDataSO = gameDataSO;
    }

    public abstract GameModeType Mode { get; }

    public void SetMatchmakingPage(MatchmakingPage page)
    {
        matchmakingPage = page;
    }

    public abstract void StartMatch();

    public virtual void CancelMatch()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void OnJoinedRoom()
    {
    }

    public virtual void OnLeft()
    {
    }

    public virtual void OnCreateRoomFailed()
    {
    }



    // Connection callbacks
    public virtual void OnConnected()
    {
    }

    public virtual void OnConnectedToMaster()
    {
    }

    public virtual void OnDisconnected(DisconnectCause cause)
    {
    }

    public virtual void OnRegionListReceived(RegionHandler regionHandler)
    {
    }

    public virtual void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
    }

    public virtual void OnCustomAuthenticationFailed(string debugMessage)
    {
    }

    // Matchmaking callbacks
    public virtual void OnFriendListUpdate(List<FriendInfo> friendList)
    {
    }

    public virtual void OnCreatedRoom()
    {
    }

    public virtual void OnJoinRoomFailed(short returnCode, string message)
    {
    }

    public virtual void OnJoinRandomFailed(short returnCode, string message)
    {
    }

    // In-room callbacks
    public virtual void OnPlayerEnteredRoom(Player newPlayer)
    {
    }

    public virtual void OnPlayerLeftRoom(Player otherPlayer)
    {
    }

    public virtual void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
    }

    public virtual void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
    }

    public virtual void OnMasterClientSwitched(Player newMasterClient)
    {
    }

    // Lobby callbacks
    public virtual void OnJoinedLobby()
    {
    }

    public virtual void OnLeftLobby()
    {
    }

    public virtual void OnRoomListUpdate(List<RoomInfo> roomList)
    {
    }

    public virtual void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
    }

    public virtual void SetProfile()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();
        if (string.IsNullOrEmpty(profileManager.GetUserName()))
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.UserNameInput);
        }
        else if (profileManager.GetProfileAvtarID() <= 0)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.AvtarSelection);
        }
    }
}
