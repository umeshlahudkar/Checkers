using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Unity.Mathematics;

public class MatchmakingConnectionManager : MonoBehaviourPunCallbacks
{
    // Custom room property used to only match players who selected the same rule set - set on
    // room creation and filtered on via a SQL lobby, so mismatched rule sets never join together.
    // "C0" is not an arbitrary choice: Photon's SQL lobby only allows filtering on its reserved
    // "C0".."C9" property names (int/string only), so this key can't be renamed to something
    // more descriptive without breaking the filter.
    private const string RuleSetPropertyKey = "C0";
    private static readonly TypedLobby RuleSetLobby = new TypedLobby("RuleSetLobby", LobbyType.SqlLobby);

    [SerializeField] private GameDataSO gameDataSO;

    private Dictionary<GameModeType, MatchModeHandler> handlers;
    private MatchModeHandler activeHandler;

    public bool IsMasterClient => PhotonNetwork.IsMasterClient;


    public bool IsConnected => PhotonNetwork.IsConnected;
    public bool IsConnectedAndReady => PhotonNetwork.IsConnectedAndReady;

    private void Awake()
    {
        if (ServiceLocator.TryGet<MatchmakingConnectionManager>(out MatchmakingConnectionManager existing) && existing != this)
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
        if (ServiceLocator.TryGet<MatchmakingConnectionManager>(out MatchmakingConnectionManager current) && current == this)
        {
            ServiceLocator.Unregister<MatchmakingConnectionManager>();
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

        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { RuleSetPropertyKey, ServiceLocator.Get<GameSettingsManager>().GetRuleSetIndex() }
        };

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = roomProperties,
            CustomRoomPropertiesForLobby = new[] { RuleSetPropertyKey }
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions, RuleSetLobby);
    }

    public bool JoinRandomRoom()
    {
        if(IsConnectedAndReady)
        {
            int ruleSetIndex = ServiceLocator.Get<GameSettingsManager>().GetRuleSetIndex();
            string sqlLobbyFilter = $"{RuleSetPropertyKey} = {ruleSetIndex}";

            return PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, RuleSetLobby, sqlLobbyFilter);
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
        Debug.Log("[MatchmakingConnectionManager] OnConnected");
        activeHandler?.OnConnected();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"[MatchmakingConnectionManager] OnConnectedToMaster - inLobby: {PhotonNetwork.InLobby}");

        if(!PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.JoinLobby(RuleSetLobby);
        }

        activeHandler?.OnConnectedToMaster();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnDisconnected - cause: {cause}");
        activeHandler?.OnDisconnected(cause);
    }

    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log("[MatchmakingConnectionManager] OnRegionListReceived");
        activeHandler?.OnRegionListReceived(regionHandler);
    }

    public override void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Debug.Log("[MatchmakingConnectionManager] OnCustomAuthenticationResponse");
        activeHandler?.OnCustomAuthenticationResponse(data);
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnCustomAuthenticationFailed - message: {debugMessage}");
        activeHandler?.OnCustomAuthenticationFailed(debugMessage);
    }

    public override void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        Debug.Log("[MatchmakingConnectionManager] OnFriendListUpdate");
        activeHandler?.OnFriendListUpdate(friendList);
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[MatchmakingConnectionManager] OnJoinedLobby");
        activeHandler?.SetProfile();
        activeHandler?.OnJoinedLobby();
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[MatchmakingConnectionManager] OnLeftLobby");
        activeHandler?.OnLeftLobby();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnRoomListUpdate - count: {roomList.Count}");
        activeHandler?.OnRoomListUpdate(roomList);
    }

    public override void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        Debug.Log("[MatchmakingConnectionManager] OnLobbyStatisticsUpdate");
        activeHandler?.OnLobbyStatisticsUpdate(lobbyStatistics);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"[MatchmakingConnectionManager] OnCreatedRoom - room: {PhotonNetwork.CurrentRoom?.Name}");
        activeHandler?.OnCreatedRoom();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnJoinRoomFailed - code: {returnCode}, message: {message}");
        activeHandler?.OnJoinRoomFailed(returnCode, message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnJoinRandomFailed - code: {returnCode}, message: {message}");
        activeHandler?.OnJoinRandomFailed(returnCode, message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[MatchmakingConnectionManager] OnJoinedRoom - room: {PhotonNetwork.CurrentRoom?.Name}, isMasterClient: {PhotonNetwork.IsMasterClient}");

        PhotonNetwork.AutomaticallySyncScene = true;

        activeHandler?.OnJoinedRoom();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnCreateRoomFailed - code: {returnCode}, message: {message}");

        activeHandler?.OnCreateRoomFailed();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnPlayerEnteredRoom - player: {newPlayer.NickName}, playerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");
        activeHandler?.OnPlayerEnteredRoom(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnPlayerLeftRoom - player: {otherPlayer.NickName}");
        activeHandler?.OnPlayerLeftRoom(otherPlayer);
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Debug.Log("[MatchmakingConnectionManager] OnRoomPropertiesUpdate");
        activeHandler?.OnRoomPropertiesUpdate(propertiesThatChanged);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnPlayerPropertiesUpdate - player: {targetPlayer.NickName}");
        activeHandler?.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[MatchmakingConnectionManager] OnMasterClientSwitched - newMasterClient: {newMasterClient.NickName}");
        activeHandler?.OnMasterClientSwitched(newMasterClient);
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[MatchmakingConnectionManager] OnLeftRoom");
        activeHandler?.OnLeft();
    }
}
