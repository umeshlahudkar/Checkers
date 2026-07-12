using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : Service<LobbyManager>
{
    [Header("Photon Room Listener")]
    [SerializeField] private PhotonRoomListener photonRoomListener;

    [Header("Matchmaking Page")]
    [SerializeField] private MatchmakingPage matchmakingPage;

    [SerializeField] private GameDataSO gameDataSO;

    private const float RoomJoinWaitTime = 10f;
    private const int OnlineStakeAmount = 250;

    private Coroutine roomJoinTimeoutCoroutine;

    private void Start()
    {
        ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.MainMenuPage);
    }

    public void SetPlayerData(Player opponentPlayer, int avtarIndex)
    {
        gameDataSO.ownPlayer.isMasterClient = PhotonNetwork.IsMasterClient;
        gameDataSO.ownPlayer.userName = ServiceLocator.Get<ProfileManager>().GetUserName();
        gameDataSO.ownPlayer.avtarIndex = ServiceLocator.Get<ProfileManager>().GetProfileAvtarIndex();

        gameDataSO.opponentPlayer.isMasterClient = opponentPlayer.IsMasterClient;
        gameDataSO.opponentPlayer.userName = opponentPlayer.NickName;
        gameDataSO.opponentPlayer.avtarIndex = avtarIndex;
    }

    public void ToggleMainMenuScreen(bool status)
    {
        if (status)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.MainMenuPage);
        }
        else
        {
            ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
        }
    }

    public void SetProfile()
    {
        ServiceLocator.Get<PersistentUI>().loadingScreen.DeactivateLoadingScreen();
        if (!ServiceLocator.Get<ProfileManager>().HasUserNameSet)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.UserNameInput);
        }
        else if(!ServiceLocator.Get<ProfileManager>().HasAvtarSet)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.AvtarSelection);
        }
    }

    public void OnQuitButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        Application.Quit();
    }

    public void OnAvtarButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.AvtarSelection);
    }

    public void StartMatch(GameMode mode)
    {
        gameDataSO.gameMode = mode;

        switch (mode)
        {
            case GameMode.Online:
                JoinRoom();
                break;

            case GameMode.PVP:
                StartCoroutine(LoadGame());
                break;

            case GameMode.PVC:
                ServiceLocator.Get<CoinManager>().DeductCoin(250, null, () =>
                {
                    StartCoroutine(LoadGame());
                });
                break;
        }
    }

    private void JoinRoom()
    {
        if(photonRoomListener.RequestJoinRandomRoom())
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Matchmaking);
            roomJoinTimeoutCoroutine = StartCoroutine(RoomJoinTimeout());
        }
        else
        {
            ServiceLocator.Get<PersistentUI>().massageDisplay.ShowMassage("No internet connection!");
        }
    }

    private IEnumerator RoomJoinTimeout()
    {
        yield return new WaitForSeconds(RoomJoinWaitTime);

        roomJoinTimeoutCoroutine = null;
        photonRoomListener.LeaveRoom();
        CancelMatchmaking();
    }

    public void OnRoomJoined()
    {
        CancelRoomJoinTimeout();
    }

    public void OnMatchmakingFailed()
    {
        CancelRoomJoinTimeout();
        CancelMatchmaking();
    }

    public void OnOpponentFound(Player opponentPlayer, int avtarIndex)
    {
        CancelRoomJoinTimeout();
        SetPlayerData(opponentPlayer, avtarIndex);

        matchmakingPage.ShowOpponentFound(opponentPlayer.NickName, ServiceLocator.Get<ProfileManager>().GetAvtar(avtarIndex));

        StartCoroutine(StartOnlineMatch());
    }

    public void CancelMatchmaking()
    {
        CancelRoomJoinTimeout();
        photonRoomListener.LeaveRoom();
        ServiceLocator.Get<AudioManager>().StopMatchmakingScrollSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    private void CancelRoomJoinTimeout()
    {
        if (roomJoinTimeoutCoroutine != null)
        {
            StopCoroutine(roomJoinTimeoutCoroutine);
            roomJoinTimeoutCoroutine = null;
        }
    }

    private IEnumerator StartOnlineMatch()
    {
        yield return new WaitForSeconds(1.5f);

        ServiceLocator.Get<CoinManager>().DeductCoin(OnlineStakeAmount);

        ServiceLocator.Get<PersistentUI>().loadingScreen.ActivateLoadingScreen("Starting Match");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            PhotonNetwork.LoadLevel(1);
        }
    }

    private IEnumerator LoadGame()
    {
        ServiceLocator.Get<PersistentUI>().loadingScreen.ActivateLoadingScreen("Starting Match");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(1);
    }
}
