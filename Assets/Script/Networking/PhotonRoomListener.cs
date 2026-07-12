using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonRoomListener : MonoBehaviourPunCallbacks
{
    private void Start()
    {
        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public bool RequestJoinRandomRoom()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.JoinRandomRoom();
    }

    public void LeaveRoom()
    {
        if(PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
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
            ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        }
    }

    public override void OnJoinedLobby()
    {
        ServiceLocator.Get<LobbyManager>().SetProfile();
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
        PhotonNetwork.NickName = ServiceLocator.Get<ProfileManager>().GetUserName();

        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable["ProfileIndex"] = ServiceLocator.Get<ProfileManager>().GetProfileAvtarIndex();

        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);

        ServiceLocator.Get<LobbyManager>().OnRoomJoined();

        if(!PhotonNetwork.IsMasterClient)
        {
            Player masterPlayer = PhotonNetwork.CurrentRoom.GetPlayer(PhotonNetwork.CurrentRoom.masterClientId);
            StartCoroutine(CheckForPropertiesSet(masterPlayer));
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ServiceLocator.Get<LobbyManager>().OnMatchmakingFailed();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
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

        ServiceLocator.Get<LobbyManager>().OnOpponentFound(newPlayer, index);
    }
}
