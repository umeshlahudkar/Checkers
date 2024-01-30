using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class EventManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public void OnEvent(EventData photonEvent)
    {
        EventType type = (EventType)photonEvent.Code;

        switch(type)
        {
            case EventType.RematchConfirmation:
                UIController.Instance.ToggleRematchScreen(true);
                break;

            case EventType.RematchAccept:
                UIController.Instance.ToggleMsgScreen(true, "opponent ready to play!");
                SendRematchEvent();
                break;

            case EventType.RematchDenied:
                UIController.Instance.ToggleMsgScreen(true, "opponent not ready to play!", true);
                break;

            case EventType.Rematch:
                GameManager.Instance.Rematch();
                break;
        }
    }

    public void SendRematchEvent()
    {
        PhotonNetwork.RaiseEvent(
           (byte)EventType.Rematch,
           null,
           new RaiseEventOptions { Receivers = ReceiverGroup.All },
           SendOptions.SendReliable);
    }

    public void SendRematchConfirmationEvent()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventType.RematchConfirmation,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    public void SendRematchDeniedEvent()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventType.RematchDenied,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    public void SendRematchAcceptEvent()
    {
        PhotonNetwork.RaiseEvent(
            (byte)EventType.RematchAccept,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(0);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UIController.Instance.ToggleGameWinScreen(true);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}

public enum EventType : byte
{
    RematchConfirmation = 0,
    RematchAccept = 1,
    RematchDenied = 2,
    Rematch = 3
}
