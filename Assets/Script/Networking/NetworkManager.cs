using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class NetworkManager : MonoBehaviourPunCallbacks
{
    public MatchMakingController matchMakingManager;
    public GameDataSO gameDataSO;

    private readonly float roomJoinWaitTime = 10f;
    private float elapcedTime = 0;


    private void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Update()
    {
        if (elapcedTime > 0)
        {
            elapcedTime -= Time.deltaTime;
            if (elapcedTime <= 0)
            {
                elapcedTime = 0;
                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.LeaveRoom();
                }
                AudioManager.Instance.StopMatchmakingScrollSound();
                PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
                LobbyManager.Instance.ToggleMainMenuScreen(true);
            }
        }
    }

    public override void OnConnectedToMaster()
    {
        if(!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        else
        {
            PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        }
    }

    public override void OnJoinedLobby()
    {
        LobbyManager.Instance.SetProfile();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        string roomName = "Room " + Random.Range(1, 1000);
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = ProfileManager.Instance.GetUserName();

        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable["ProfileIndex"] = ProfileManager.Instance.GetProfileAvtarIndex();

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);

        //PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        //LobbyManager.Instance.ToggleMatchmakingScreen(true);

        if(!PhotonNetwork.IsMasterClient)
        {
            Player masterPlayer = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.CurrentRoom.masterClientId);
            StartCoroutine(CheckForPropertiesSet(masterPlayer));
        }

        elapcedTime = 0;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        elapcedTime = 0;

        AudioManager.Instance.StopMatchmakingScrollSound();
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        LobbyManager.Instance.ToggleMainMenuScreen(true);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            StartCoroutine(CheckForPropertiesSet(newPlayer));
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        //PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Connecting to network");
        //LobbyManager.Instance.ToggleMainMenuScreen(true);
    }

    private IEnumerator CheckForPropertiesSet(Player newPlayer)
    {
        elapcedTime = 0;
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

        matchMakingManager.SetPlayerFound(newPlayer.NickName, index);
        LobbyManager.Instance.SetPlayerData(newPlayer, index);
    }

   
    public bool JoinRandomRoom()
    {
        if(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.JoinRandomRoom())
        {
            elapcedTime = roomJoinWaitTime;
            return true;
        }
        return false;
    }

}
