using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class MatchSessionEventManager : MonoBehaviourPunCallbacks
{
    public override void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        GamePageManager pageManager = ServiceLocator.Get<GamePageManager>();
        bool canOpenGameOverScreen = !pageManager.IsResultShowing
            // A GameOver/Draw RPC that already resolved the match this same frame (see H5/H6, and
            // GameManager.GameOver/Draw's own matching gameState guard) may not have finished
            // opening its result yet even though the match is already over - checking GameState
            // directly closes that gap instead of relying solely on page-open state.
            && ServiceLocator.Get<GameManager>().GameState == GameState.Playing;

        if(canOpenGameOverScreen)
        {
            StartCoroutine(ServiceLocator.Get<GameManager>().PlayForfeitSequence());
        }
        else if (ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer
            && ServiceLocator.Get<GameManager>().GameState == GameState.Ending)
        {
            // The match was already over (result on screen or about to be) - the only thing the
            // opponent leaving changes now is that a rematch can no longer happen.
            ServiceLocator.Get<GameManager>().OnOpponentLeftAfterMatch();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (newMasterClient.IsLocal && ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer)
        {
            ServiceLocator.Get<GameManager>().OnBecameMaster();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if(ServiceLocator.Get<GameManager>().GameMode == GameModeType.Multiplayer && ServiceLocator.Get<GameManager>().GameState != GameState.Ending)
        {
            // The toast lives on the DDOL canvas, so it stays up through the menu load below.
            ServiceLocator.Get<WarningNotifier>().Show(MatchmakingConnectionManager.GetDisconnectMessage(cause));
            StartCoroutine(ServiceLocator.Get<GameManager>().LoadMainMenu());
        }
    }

    // If the master client is the one backgrounded, it can't police the turn timer (its own Update
    // loop is suspended), and nothing switches the turn until Photon's disconnect grace period
    // (PhotonNetwork.KeepAliveInBackground, ~60s) expires and auto-promotes the other client. Handing
    // master off immediately on minimize closes that gap instead of waiting on it. This is the hook
    // that actually fires for a mobile home-button/task-switcher minimize - OnApplicationFocus below
    // is not reliably raised by that on Android/iOS, so it stays only as a desktop alt-tab backstop;
    // both share the same guard, and calling SetMasterClient a second time once this client is no
    // longer master is already a safe no-op via the IsMasterClient check.
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

    private void OnApplicationFocus(bool focus)
    {
        if (focus || ServiceLocator.Get<GameManager>().GameMode != GameModeType.Multiplayer || !PhotonNetwork.IsMasterClient)
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
