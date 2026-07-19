using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchSessionEventManager : MonoBehaviourPunCallbacks, IOnEventCallback
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
                break;

            case EventType.RematchAccept:
                if(isReadyToRematch)
                {
                    confirmationElapcedTime = 0;
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
                ServiceLocator.Get<CoinManager>().DeductCoin(250, null, () =>
                {
                    ServiceLocator.Get<AudioManager>().StopTimeTickingSound();
                    ServiceLocator.Get<GameManager>().StartRematch();
                });
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
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        bool canOpenGameOverScreen = !ServiceLocator.Get<GamePageManager>().IsPageOpen(GamePageType.ResultPage);

        if(canOpenGameOverScreen)
        {
            ServiceLocator.Get<GameManager>().SetGameOver();
            ServiceLocator.Get<CoinManager>().AddCoin(500);
            ServiceLocator.Get<GamePageManager>().ResultPage.ShowVictoryByForfeit(500);
            ServiceLocator.Get<GamePageManager>().OpenPageAsOverlay(GamePageType.ResultPage);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer && ServiceLocator.Get<GameManager>().GameState != GameState.Ending)
        {
            StartCoroutine(ServiceLocator.Get<GameManager>().LoadMainMenu());
        }
    }

    // If the master client is the one backgrounded, it can't police the turn timer (its own Update
    // loop is suspended), and nothing switches the turn until Photon's disconnect grace period
    // (PhotonNetwork.KeepAliveInBackground, ~60s) expires and auto-promotes the other client. Handing
    // master off immediately on minimize closes that gap instead of waiting on it.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus || ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer || !PhotonNetwork.IsMasterClient)
        {
            return;
        }

        if (PhotonNetwork.PlayerListOthers.Length > 0)
        {
            PhotonNetwork.SetMasterClient(PhotonNetwork.PlayerListOthers[0]);
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
