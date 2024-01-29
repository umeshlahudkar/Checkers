using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static bool hasUsernameSet = false;
    public LobbyUIController lobbyUIController;
    public MatchMakingManager matchMakingManager;
    public GameDataSO gameDataSO;

    private void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        lobbyUIController.ToggleConnectingScreen(false);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;
        string roomName = "Room " + Random.Range(1, 1000);
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnCreatedRoom()
    {
        
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Room joined " + PhotonNetwork.CurrentRoom.Name + " " + PhotonNetwork.NickName);
        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable["ProfileIndex"] = ProfileManager.Instance.GetProfileAvtarIndex();

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);

        lobbyUIController.ToggleMatchmakingScreen(true);

        if(!PhotonNetwork.IsMasterClient)
        {
            Player masterPlayer = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.CurrentRoom.masterClientId);
            StartCoroutine(CheckForPropertiesSet(masterPlayer));
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room creation failed " + message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Other player joined " + newPlayer.NickName);
        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            StartCoroutine(CheckForPropertiesSet(newPlayer));
        }
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
        Debug.Log("Other player joined " + newPlayer.NickName + "......." + index);
        matchMakingManager.SetPlayerFound(newPlayer.NickName, index);
        SaveGameData(newPlayer, index);
    }

    private void SaveGameData(Player opponentPlayer, int avtarIndex)
    {
        gameDataSO.ownPlayer.isMasterClient = PhotonNetwork.IsMasterClient;
        gameDataSO.ownPlayer.userName = ProfileManager.Instance.GetUserName();
        gameDataSO.ownPlayer.avtarIndex = ProfileManager.Instance.GetProfileAvtarIndex();

        gameDataSO.opponentPlayer.isMasterClient = opponentPlayer.IsMasterClient;
        gameDataSO.opponentPlayer.userName = opponentPlayer.NickName;
        gameDataSO.opponentPlayer.avtarIndex = avtarIndex;
    }


    public void SetUserName(string username)
    {
        if(PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = username;
            hasUsernameSet = true;
        }
    }

    public void JoinRandomRoom()
    {
        if(PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinRandomRoom();
        }
    }

}
