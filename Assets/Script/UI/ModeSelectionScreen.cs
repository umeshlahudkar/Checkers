using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class ModeSelectionScreen : MonoBehaviour
{
    [SerializeField] private LobbyUIController lobbyUIController;
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private Image[] buttons;
    [SerializeField] private Transform playBtnCoinImg;

    [SerializeField] private GameObject playButtonWithCoin;
    [SerializeField] private GameObject playButtonWithoutCoin;

    private GameMode selectedGamemode;
    private Color originalColor = new(1, 1, 1, 0.5f);
    private Color selectedColor = new(1, 1, 1, 1);

    private void OnEnable()
    {
        selectedGamemode = GameMode.None;
        //ResetButton();
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = originalColor;
        }
    }

    public void OnModeButtonClick(int buttonNumber)
    {
        AudioManager.Instance.PlayButtonClickSound();
        ResetButton();
        buttons[buttonNumber - 1].color = selectedColor;
        buttons[buttonNumber - 1].transform.DOScale(Vector3.one * 0.9f, 0.1f);
        selectedGamemode = (GameMode)buttonNumber;

        if(selectedGamemode == GameMode.Online || selectedGamemode == GameMode.PVC)
        {
            playButtonWithoutCoin.SetActive(false);
            playButtonWithCoin.SetActive(true);
        }
        else
        {
            playButtonWithoutCoin.SetActive(true);
            playButtonWithCoin.SetActive(false);
        }
    }

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
                    //faderScreen.SetActive(false);
                    //gameObject.SetActive(false);
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

    private IEnumerator LoadGame()
    {
        PersistentUI.Instance.loadingScreen.ActivateLoadingScreen("Starting Match");

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene(1);
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.Deactivate();
    }

    private void ResetButton()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].color = originalColor;
            buttons[i].transform.DOScale(Vector3.one, 0.1f);
        }
    }
}
