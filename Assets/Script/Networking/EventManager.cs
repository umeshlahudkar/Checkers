using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ExitGames.Client.Photon;

public class EventManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private readonly float rematchConfirmationWiatTime = 10f;
    private float confirmationElapcedTime = 0;

    private readonly float rematchConfirmationAcknoTime = 10f;
    private float confirmationAcknoElapcedTime = 0;

    private bool isReadyToRematch = false;

    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Update()
    {
        if(confirmationElapcedTime > 0)
        {
            confirmationElapcedTime -= Time.deltaTime;
            if(confirmationElapcedTime <= 0)
            {
                confirmationElapcedTime = 0;
                isReadyToRematch = false;
                OnRematchDenied();
            }
        }

        if(confirmationAcknoElapcedTime > 0)
        {
            confirmationAcknoElapcedTime -= Time.deltaTime;
            if (confirmationAcknoElapcedTime <= 0)
            {
                confirmationAcknoElapcedTime = 0;
                OnRematchDenied();
            }
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        EventType type = (EventType)photonEvent.Code;

        switch(type)
        {
            case EventType.RematchConfirmation:
                ServiceLocator.Get<GameplayUIController>().DisableAllScreen();
                ServiceLocator.Get<GameplayUIController>().ToggleRematchScreen(true);
                break;

            case EventType.RematchAccept:
                if(isReadyToRematch)
                {
                    confirmationElapcedTime = 0;
                    ServiceLocator.Get<GameplayUIController>().DisableAllScreen();
                    ServiceLocator.Get<GameplayUIController>().ToggleMsgScreen(true, "opponent ready to play!");
                    SendRematchEvent();
                }
                else
                {
                    SendRematchDeniedEvent();
                }
                break;

            case EventType.RematchDenied:
                OnRematchDenied();
                break;

            case EventType.Rematch:
                confirmationAcknoElapcedTime = 0;
                //StartCoroutine(ServiceLocator.Get<GameManager>().Rematch());
                ServiceLocator.Get<GameplayUIController>().RematchForOnlineMode();
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
        confirmationElapcedTime = rematchConfirmationWiatTime;
        isReadyToRematch = true;

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
        confirmationAcknoElapcedTime = rematchConfirmationAcknoTime;

        PhotonNetwork.RaiseEvent(
            (byte)EventType.RematchAccept,
            null,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    private void OnRematchDenied()
    {
        ServiceLocator.Get<GameplayUIController>().DisableAllScreen();
        ServiceLocator.Get<GameplayUIController>().ToggleMsgScreen(true, "opponent not ready to play!", true);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer && ServiceLocator.Get<GameManager>().IsReadyToLeaveGameplay)
        {
            //StartCoroutine(ServiceLocator.Get<GameplayUIController>().LoadMainMenu());
        }
       
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if(ServiceLocator.Get<GameplayUIController>().CanOpenGameOverScreen())
        {
            ServiceLocator.Get<GameManager>().SetGameOver();
            ServiceLocator.Get<GameplayUIController>().ToggleGameWinScreen(true);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer && ServiceLocator.Get<GameManager>().GameState != GameState.Ending)
        {
            ServiceLocator.Get<PersistentUI>().massageDisplay.ShowMassage("Connection lost!");
            StartCoroutine(ServiceLocator.Get<GameplayUIController>().LoadMainMenu());
        }
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
