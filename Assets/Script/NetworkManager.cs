using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
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
        Debug.Log("lobby joined");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Random Room joining failed");
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 2;

        string roomName = "Room " + Random.Range(1, 1000);

        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Room joined " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.Log("Room creation failed " + message);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Other player joined");

        if(PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            PhotonNetwork.LoadLevel(1);
        }
    }

    public void OnPlayButtonClick()
    {
        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Play button click");
    }
}
