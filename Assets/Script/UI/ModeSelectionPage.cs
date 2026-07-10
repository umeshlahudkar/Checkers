using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeSelectionPage : Page
{
    [SerializeField] private LobbyUIController lobbyUIController;

    [SerializeField] private ModeListSO modeList;
    [SerializeField] private ModeTile tileTemplate;
    [SerializeField] private Transform tileContainer;

    [SerializeField] private GameMode selectedGamemode;
    private bool tilesCreated;

    protected override void OnOpened()
    {
        selectedGamemode = GameMode.None;
        CreateTiles();
    }

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        foreach (ModeInfo info in modeList.modes)
        {
            ModeTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(info, OnModeSelected);
        }

        tilesCreated = true;
    }

    private void OnModeSelected(ModeInfo info)
    {
        AudioManager.Instance.PlayButtonClickSound();
        selectedGamemode = info.mode;
        StartCoroutine(LoadGame());
    }

    /*
    public void OnPlayButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        if (selectedGamemode != GameMode.None)
        {
            lobbyUIController.SetGameMode(selectedGamemode);

            switch (selectedGamemode)
            {
                case GameMode.Online:
                    lobbyUIController.JoinRoom();
                    break;

                case GameMode.PVP:
                    StartCoroutine(LoadGame());
                    break;

                case GameMode.PVC:
                    CoinManager.Instance.DeductCoin(250, playBtnCoinImg, () =>
                    {
                        StartCoroutine(LoadGame());
                    });
                    break;
            }
        }
    }
    */

    private IEnumerator LoadGame()
    {
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Starting Match");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(1);
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.CloseCurrentPage();
    }

    public void OnBackButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.GoBack();
    }
}
