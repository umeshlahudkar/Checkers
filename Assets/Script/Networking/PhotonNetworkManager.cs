using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Unity.Mathematics;

public class PhotonNetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameDataSO gameDataSO;

    private Dictionary<GameModeType, MatchModeHandler> handlers;
    private MatchModeHandler activeHandler;

    public bool IsMasterClient => PhotonNetwork.IsMasterClient;


    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsConnectedAndReady => PhotonNetwork.IsConnectedAndReady;

    private void Awake()
    {
        if (ServiceLocator.TryGet<PhotonNetworkManager>(out PhotonNetworkManager existing) && existing != this)
        {
            Destroy(gameObject);
            return;
        }

        ServiceLocator.Register(this);

        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.OfflineMode = false;
        }

        handlers = new Dictionary<GameModeType, MatchModeHandler>
        {
            { GameModeType.Multiplayer, new OnlineModeHandler(this, gameDataSO) },
            { GameModeType.VsPlayer, new PvpModeHandler(this, gameDataSO) },
            { GameModeType.VsBot, new PvcModeHandler(this, gameDataSO) },
        };

        // Default receiver for connection-lifecycle callbacks that fire before any mode is chosen.
        activeHandler = null;
    }

    private void Update()
    {
        activeHandler?.Update();
    }

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<PhotonNetworkManager>(out PhotonNetworkManager current) && current == this)
        {
            ServiceLocator.Unregister<PhotonNetworkManager>();
        }
    }

    public void StartMatch(GameModeType mode)
    {
        if (handlers.TryGetValue(mode, out MatchModeHandler handler))
        {
            MatchmakingPage page = (MatchmakingPage)ServiceLocator.Get<MenuPageManager>().GetPage(MenuPageType.Matchmaking);

            activeHandler = handler;
            handler.SetMatchmakingPage(page);
            handler.StartMatch();
        }
        else
        {
            Debug.LogError($"No mode handler registered for {mode}");
        }
    }

    public void CancelMatch()
    {
        activeHandler?.CancelMatch();
    }

    public void Connect()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.NickName = profileManager.UserName;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "avtarID",  profileManager.AvatarID},
            { "userName", profileManager.UserName }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        string roomName = "Online_" + (UnityEngine.Random.Range(1000, 9999)).ToString();

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public bool JoinRandomRoom()
    {
        if(IsConnectedAndReady)
        {
            return PhotonNetwork.JoinRandomRoom();
        }

        return false;
    }

    public bool IsRoomFull => PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers;

    public PlayerInfo GetOwnPlayerInfo()
    {
        return BuildPlayerInfo(PhotonNetwork.LocalPlayer);
    }

    public PlayerInfo GetOpponentPlayerInfo()
    {
        foreach (Player player in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (!player.IsLocal)
            {
                return BuildPlayerInfo(player);
            }
        }

        return default;
    }

    private PlayerInfo BuildPlayerInfo(Player player)
    {
        int avtarIndex = player.CustomProperties.TryGetValue("avtarID", out object avtarID) ? (int)avtarID : -1;

        return new PlayerInfo
        {
            userName = player.NickName,
            avatar = ServiceLocator.Get<ProfileManager>().GetAvtar(avtarIndex)
        };
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void CloseRoomAndLoadOnlineScene(string sceneName)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel(sceneName);
    }


    public void Disconnect()
    {
        PhotonNetwork.Disconnect();
    }

    public override void OnConnected()
    {
        Debug.Log("[PhotonNetworkManager] OnConnected");
        activeHandler?.OnConnected();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"[PhotonNetworkManager] OnConnectedToMaster - inLobby: {PhotonNetwork.InLobby}");
        activeHandler?.OnConnectedToMaster();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PhotonNetworkManager] OnDisconnected - cause: {cause}");
        activeHandler?.OnDisconnected(cause);
    }

    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("[PhotonNetworkManager] OnRegionListReceived");
        activeHandler?.OnRegionListReceived(regionHandler);
    }

    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("[PhotonNetworkManager] OnCustomAuthenticationResponse");
        activeHandler?.OnCustomAuthenticationResponse(data);
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log($"[PhotonNetworkManager] OnCustomAuthenticationFailed - message: {debugMessage}");
        activeHandler?.OnCustomAuthenticationFailed(debugMessage);
    }

    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("[PhotonNetworkManager] OnFriendListUpdate");
        activeHandler?.OnFriendListUpdate(friendList);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[PhotonNetworkManager] OnJoinedLobby");
        activeHandler?.SetProfile();
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[PhotonNetworkManager] OnLeftLobby");
        activeHandler?.OnLeftLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[PhotonNetworkManager] OnRoomListUpdate - count: {roomList.Count}");
        activeHandler?.OnRoomListUpdate(roomList);
    }

    public override void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        Debug.Log("[PhotonNetworkManager] OnLobbyStatisticsUpdate");
        activeHandler?.OnLobbyStatisticsUpdate(lobbyStatistics);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[PhotonNetworkManager] OnCreatedRoom - room: {PhotonNetwork.CurrentRoom?.Name}");
        activeHandler?.OnCreatedRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[PhotonNetworkManager] OnJoinRoomFailed - code: {returnCode}, message: {message}");
        activeHandler?.OnJoinRoomFailed(returnCode, message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[PhotonNetworkManager] OnJoinRandomFailed - code: {returnCode}, message: {message}");
        activeHandler?.OnJoinRandomFailed(returnCode, message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonNetworkManager] OnJoinedRoom - room: {PhotonNetwork.CurrentRoom?.Name}, isMasterClient: {PhotonNetwork.IsMasterClient}");

        PhotonNetwork.AutomaticallySyncScene = true;

        activeHandler?.OnJoinedRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[PhotonNetworkManager] OnCreateRoomFailed - code: {returnCode}, message: {message}");

        activeHandler?.OnCreateRoomFailed();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[PhotonNetworkManager] OnPlayerEnteredRoom - player: {newPlayer.NickName}, playerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");
        activeHandler?.OnPlayerEnteredRoom(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[PhotonNetworkManager] OnPlayerLeftRoom - player: {otherPlayer.NickName}");
        activeHandler?.OnPlayerLeftRoom(otherPlayer);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("[PhotonNetworkManager] OnRoomPropertiesUpdate");
        activeHandler?.OnRoomPropertiesUpdate(propertiesThatChanged);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"[PhotonNetworkManager] OnPlayerPropertiesUpdate - player: {targetPlayer.NickName}");
        activeHandler?.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[PhotonNetworkManager] OnMasterClientSwitched - newMasterClient: {newMasterClient.NickName}");
        activeHandler?.OnMasterClientSwitched(newMasterClient);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[PhotonNetworkManager] OnLeftRoom");
        activeHandler?.OnLeft();
    }
}
