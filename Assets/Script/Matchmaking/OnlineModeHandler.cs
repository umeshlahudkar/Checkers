using System.Collections;
using UnityEngine;
using Photon.Realtime;

public class OnlineModeHandler : MatchModeHandler
{
    private const float RoomJoinWaitTime = 10f;
    private const int OnlineStakeAmount = 250;

    private Coroutine roomJoinTimeoutCoroutine;

    public OnlineModeHandler(PhotonNetworkManager photonNetworkManager, GameDataSO gameDataSO)
        : base(photonNetworkManager, gameDataSO)
    {
    }

    public override GameModeType Mode => GameModeType.Multiplayer;

    public override void StartMatch()
    {
        gameDataSO.gameMode = Mode;

        if (photonNetworkManager.RequestJoinRandomRoom())
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Matchmaking);
            roomJoinTimeoutCoroutine = photonNetworkManager.RunCoroutine(RoomJoinTimeout());
        }
    }

    private IEnumerator RoomJoinTimeout()
    {
        yield return new WaitForSeconds(RoomJoinWaitTime);

        roomJoinTimeoutCoroutine = null;
        photonNetworkManager.LeaveRoom();
        CancelMatch();
    }

    public override void OnJoinedRoom()
    {
        CancelRoomJoinTimeout();
    }

    public override void OnCreateRoomFailed()
    {
        CancelRoomJoinTimeout();
        CancelMatch();
    }

    public override void OnOpponentFound(Player opponentPlayer, int avtarIndex)
    {
        CancelRoomJoinTimeout();
        SetPlayerData(opponentPlayer, avtarIndex);

        matchmakingPage.ShowOpponentFound(opponentPlayer.NickName, ServiceLocator.Get<ProfileManager>().GetAvtar(avtarIndex));

        photonNetworkManager.CloseRoomAndLoadOnlineScene("GameplayScene");
    }

    private void SetPlayerData(Player opponentPlayer, int avtarIndex)
    {
        gameDataSO.ownPlayer.isMasterClient = photonNetworkManager.IsMasterClient;
        gameDataSO.ownPlayer.userName = ServiceLocator.Get<ProfileManager>().GetUserName();
        gameDataSO.ownPlayer.avtarIndex = ServiceLocator.Get<ProfileManager>().GetProfileAvtarID();

        gameDataSO.opponentPlayer.isMasterClient = opponentPlayer.IsMasterClient;
        gameDataSO.opponentPlayer.userName = opponentPlayer.NickName;
        gameDataSO.opponentPlayer.avtarIndex = avtarIndex;
    }

    public override void CancelMatch()
    {
        CancelRoomJoinTimeout();
        photonNetworkManager.LeaveRoom();
        ServiceLocator.Get<AudioManager>().StopMatchmakingScrollSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    private void CancelRoomJoinTimeout()
    {
        if (roomJoinTimeoutCoroutine != null)
        {
            photonNetworkManager.StopRunningCoroutine(roomJoinTimeoutCoroutine);
            roomJoinTimeoutCoroutine = null;
        }
    }

    private IEnumerator StartOnlineMatch()
    {
        yield return new WaitForSeconds(1.5f);

        ServiceLocator.Get<CoinManager>().DeductCoin(OnlineStakeAmount);

        photonNetworkManager.CloseRoomAndLoadOnlineScene("GameplayScene");
    }
}
