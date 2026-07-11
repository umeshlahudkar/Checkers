using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : Singleton<LobbyManager>
{
    [Header("Network Manager")]
    [SerializeField] private NetworkManager networkManager;

    [SerializeField] private GameDataSO gameDataSO;

    private void Start()
    {
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        MenuPageManager.Instance.OpenPage(MenuPageType.MainMenuPage);
    }

    public void SetPlayerData(Player opponentPlayer, int avtarIndex)
    {
        gameDataSO.ownPlayer.isMasterClient = PhotonNetwork.IsMasterClient;
        gameDataSO.ownPlayer.userName = ProfileManager.Instance.GetUserName();
        gameDataSO.ownPlayer.avtarIndex = ProfileManager.Instance.GetProfileAvtarIndex();

        gameDataSO.opponentPlayer.isMasterClient = opponentPlayer.IsMasterClient;
        gameDataSO.opponentPlayer.userName = opponentPlayer.NickName;
        gameDataSO.opponentPlayer.avtarIndex = avtarIndex;
    }

    public void ToggleMainMenuScreen(bool status)
    {
        if (status)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.MainMenuPage);
        }
        else
        {
            MenuPageManager.Instance.CloseCurrentPage();
        }
    }

    public void SetProfile()
    {
        PersistentUI.Instance.loadingScreen.DeactivateLoadingScreen();
        if (!ProfileManager.Instance.HasUserNameSet)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.UserNameInput);
        }
        else if(!ProfileManager.Instance.HasAvtarSet)
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.AvtarSelection);
        }
    }

    public void OnQuitButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Application.Quit();
    }

    public void OnAvtarButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.AvtarSelection);
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
                CoinManager.Instance.DeductCoin(250, null, () =>
                {
                    StartCoroutine(LoadGame());
                });
                break;
        }
    }

    private void JoinRoom()
    {
        if(networkManager.JoinRandomRoom())
        {
            MenuPageManager.Instance.OpenPage(MenuPageType.Matchmaking);
        }
        else
        {
            PersistentUI.Instance.massageDisplay.ShowMassage("No internet connection!");
        }
    }

    private IEnumerator LoadGame()
    {
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Starting Match");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(1);
    }
}
