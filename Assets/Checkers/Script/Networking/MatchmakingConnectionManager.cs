using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Unity.Mathematics;

public class MatchmakingConnectionManager : MonoBehaviourPunCallbacks
{
    private static readonly TypedLobby RuleSetLobby = new TypedLobby("RuleSetLobby", LobbyType.SqlLobby);

    [SerializeField] private GameDataSO gameDataSO;

    private Dictionary<GameModeType, MatchModeHandler> handlers;
    private MatchModeHandler activeHandler;
    private PrivateRoomModeHandler privateRoomHandler;

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

        privateRoomHandler = new PrivateRoomModeHandler(this, gameDataSO);

        handlers = new Dictionary<GameModeType, MatchModeHandler>
        {
            { GameModeType.Multiplayer, new OnlineModeHandler(this, gameDataSO) },
            { GameModeType.VsPlayer, new PvpModeHandler(this, gameDataSO) },
            { GameModeType.VsBot, new PvcModeHandler(this, gameDataSO) },
            { GameModeType.PrivateRoom, privateRoomHandler },
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

    // The guest's way in: unlike every other mode this is not started from a mode tile, it starts
    // from the Join popup once a code has been typed, so it has its own entry point rather than
    // StartMatch. Results come back through the two callbacks (the popup is still open until the room
    // is actually joined) instead of through a page change.
    public void JoinPrivateRoom(string code, System.Action onJoined, System.Action<string> onFailed)
    {
        activeHandler = privateRoomHandler;
        privateRoomHandler.SetMatchmakingPage((MatchmakingPage)ServiceLocator.Get<MenuPageManager>().GetPage(MenuPageType.Matchmaking));
        privateRoomHandler.StartJoin(code, onJoined, onFailed);
    }

    // Backing out of the Join popup after an attempt was made. Goes to the private handler directly
    // rather than through activeHandler, which may still be whichever mode was played last.
    public void CancelPrivateRoomJoin()
    {
        privateRoomHandler.CancelMatch();
    }

    public void Connect()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.NickName = profileManager.UserName;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { GameConstants.PhotonPlayerProperties.AvatarId, profileManager.AvatarID },
            { GameConstants.PhotonPlayerProperties.UserName, profileManager.UserName },
            { GameConstants.PhotonPlayerProperties.PieceType, profileManager.GetProfilePieceID() }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        string roomName = "Online_" + (UnityEngine.Random.Range(1000, 9999)).ToString();

        int pieceTypeIndex = ServiceLocator.Get<ProfileManager>().GetProfilePieceID();

        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { GameConstants.RoomMatchProperties.RuleSet, ServiceLocator.Get<GameSettingsManager>().GetRuleSetIndex() },
            { GameConstants.RoomMatchProperties.PieceColor, pieceTypeIndex }
        };

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = roomProperties,
            CustomRoomPropertiesForLobby = new[] { GameConstants.RoomMatchProperties.RuleSet, GameConstants.RoomMatchProperties.PieceColor }
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions, RuleSetLobby);
    }

    // A private room is named by its code and kept out of the random-match lists (IsVisible false),
    // so strangers matchmaking for the same ruleset can never land in it. It still carries the
    // ruleset and the host's piece color so the guest can adopt both after joining.
    public void CreatePrivateRoom(string code)
    {
        ExitGames.Client.Photon.Hashtable roomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { GameConstants.RoomMatchProperties.RuleSet, ServiceLocator.Get<GameSettingsManager>().GetRuleSetIndex() },
            { GameConstants.RoomMatchProperties.PieceColor, ServiceLocator.Get<ProfileManager>().GetProfilePieceID() }
        };

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = false,
            IsOpen = true,
            CustomRoomProperties = roomProperties
        };

        PhotonNetwork.CreateRoom(code, roomOptions);
    }

    public bool JoinRoomByCode(string code)
    {
        return PhotonNetwork.JoinRoom(code);
    }

    // Index into the ruleset list the host chose, or -1 if the room doesn't carry one.
    public int GetRoomRuleSetIndex()
    {
        if (PhotonNetwork.CurrentRoom != null
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameConstants.RoomMatchProperties.RuleSet, out object ruleSetIndex))
        {
            return (int)ruleSetIndex;
        }

        return -1;
    }

    // Random matchmaking guarantees opposite colors by filtering on them; a private room can't, since
    // the guest's profile color is whatever it is. So the guest takes the opposite of the host's after
    // joining - overriding its own preference for this one match - and the host waits for that to land
    // before starting (see OpponentHasOppositePieceType).
    public void MatchOwnPieceColorToRoom()
    {
        if (PhotonNetwork.CurrentRoom != null
            && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GameConstants.RoomMatchProperties.PieceColor, out object hostColor))
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { GameConstants.PhotonPlayerProperties.PieceType, GetOppositePieceTypeIndex((int)hostColor) }
            });
        }
    }

    public bool OpponentHasOppositePieceType()
    {
        PieceType own = GetOwnPlayerInfo().pieceType;
        PieceType opponent = GetOpponentPlayerInfo().pieceType;
        return own != PieceType.None && opponent != PieceType.None && own != opponent;
    }

    public bool JoinRandomRoom()
    {
        if(IsConnectedAndReady)
        {
            int ruleSetIndex = ServiceLocator.Get<GameSettingsManager>().GetRuleSetIndex();
            int myPieceTypeIndex = ServiceLocator.Get<ProfileManager>().GetProfilePieceID();
            int requiredOpponentPieceTypeIndex = GetOppositePieceTypeIndex(myPieceTypeIndex);
            string sqlLobbyFilter = $"{GameConstants.RoomMatchProperties.RuleSet} = {ruleSetIndex} AND {GameConstants.RoomMatchProperties.PieceColor} = {requiredOpponentPieceTypeIndex}";

            return PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, RuleSetLobby, sqlLobbyFilter);
        }

        return false;
    }

    private static int GetOppositePieceTypeIndex(int pieceTypeIndex)
    {
        return pieceTypeIndex == (int)PieceType.White ? (int)PieceType.Black : (int)PieceType.White;
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
        int avtarIndex = player.CustomProperties.TryGetValue(GameConstants.PhotonPlayerProperties.AvatarId, out object avtarID) ? (int)avtarID : -1;
        PieceType pieceType = player.CustomProperties.TryGetValue(GameConstants.PhotonPlayerProperties.PieceType, out object pieceTypeValue) ? (PieceType)(int)pieceTypeValue : PieceType.None;

        return new PlayerInfo
        {
            userName = player.NickName,
            avatar = ServiceLocator.Get<ProfileManager>().GetAvtar(avtarIndex),
            pieceType = pieceType
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

    // Player-facing text for an unexpected disconnect, shared by matchmaking and gameplay so both
    // read the same instead of showing the raw DisconnectCause name.
    public static string GetDisconnectMessage(DisconnectCause cause)
    {
        switch (cause)
        {
            case DisconnectCause.MaxCcuReached:
                return "Servers are busy. Try again soon.";

            case DisconnectCause.ExceptionOnConnect:
            case DisconnectCause.Exception:
            case DisconnectCause.ServerTimeout:
            case DisconnectCause.ClientTimeout:
                return "Connection lost. Check your internet.";

            default:
                return "Connection lost. Please try again.";
        }
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
