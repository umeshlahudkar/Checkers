using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System;
using ExitGames.Client.Photon;

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

    private void OnDestroy()
    {
        if (ServiceLocator.TryGet<PhotonNetworkManager>(out PhotonNetworkManager current) && current == this)
        {
            ServiceLocator.Unregister<PhotonNetworkManager>();
        }
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.MainMenuPage);
    }

    public Coroutine RunCoroutine(IEnumerator routine)
    {
        return StartCoroutine(routine);
    }

    public void StopRunningCoroutine(Coroutine routine)
    {
        StopCoroutine(routine);
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

    public void SetProfile()
    {
        activeHandler?.SetProfile();
    }

    public bool RequestJoinRandomRoom()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.JoinRandomRoom();
    }

    private Action onDisconnected;

    public void StartOfflineMatch()
    {
        if(IsConnected)
        {
            onDisconnected = CreateOfflineRom;
            Disconnect();
        }
        else
        {
            onDisconnected = null;
            CreateOfflineRom();
        }
    }

    private void CreateOfflineRom()
    {
        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.NickName = profileManager.UserName;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { "avtarID",  profileManager.AvatarID},
            { "userName", profileManager.UserName }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.OfflineMode = false;
            return;
        }

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


    private void Disconnect()
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

        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        }

        activeHandler?.OnConnectedToMaster();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.Log($"[PhotonNetworkManager] OnDisconnected - cause: {cause}");

        if(cause == DisconnectCause.DisconnectByClientLogic)
        {
            onDisconnected?.Invoke();
        }
        else
        {
            activeHandler?.OnDisconnected(cause);
        }
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
        Debug.Log($"[PhotonNetworkManager] OnJoinRandomFailed - code: {returnCode}, message: {message} - creating a new room");

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        string roomName = "Room " + UnityEngine.Random.Range(1, 1000);
        PhotonNetwork.CreateRoom(roomName, options);

        activeHandler?.OnJoinRandomFailed(returnCode, message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[PhotonNetworkManager] OnJoinedRoom - room: {PhotonNetwork.CurrentRoom?.Name}, offline: {PhotonNetwork.OfflineMode}, isMasterClient: {PhotonNetwork.IsMasterClient}");

        PhotonNetwork.AutomaticallySyncScene = true;

        if (PhotonNetwork.OfflineMode)
        {
            activeHandler?.OnJoinedRoom();
            return;
        }

        PhotonNetwork.NickName = ServiceLocator.Get<ProfileManager>().GetUserName();

        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable["ProfileIndex"] = ServiceLocator.Get<ProfileManager>().GetProfileAvtarID();

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);

        activeHandler?.OnJoinedRoom();

        if (!PhotonNetwork.IsMasterClient)
        {
            Player masterPlayer = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.CurrentRoom.masterClientId);
            StartCoroutine(CheckForPropertiesSet(masterPlayer));
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log($"[PhotonNetworkManager] OnCreateRoomFailed - code: {returnCode}, message: {message}");

        activeHandler?.OnCreateRoomFailed();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[PhotonNetworkManager] OnPlayerEnteredRoom - player: {newPlayer.NickName}, playerCount: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            StartCoroutine(CheckForPropertiesSet(newPlayer));
        }
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

    private IEnumerator CheckForPropertiesSet(Player newPlayer)
    {
        int index;
        while (true)
        {
            if (newPlayer.CustomProperties.TryGetValue("ProfileIndex", out object profileIndexObj))
            {
                index = (int)profileIndexObj;
                break;
            }
            yield return null;
        }

        Debug.Log($"[PhotonNetworkManager] Opponent found - player: {newPlayer.NickName}, avtarIndex: {index}");

        activeHandler?.OnOpponentFound(newPlayer, index);
    }
}
