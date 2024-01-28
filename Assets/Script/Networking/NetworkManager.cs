using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static bool hasUsernameSet = false;
    public LobbyUIController lobbyUIController;

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
        lobbyUIController.ToggleMatchmakingScreen(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room creation failed " + message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Other player joined " + newPlayer.NickName);

        if(PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            PhotonNetwork.LoadLevel(1);
        }
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
