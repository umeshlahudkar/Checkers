using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameplayUIController : Singleton<GameplayUIController>
{
    [Header("Player1 profile")]
    [SerializeField] private TextMeshProUGUI player1_nameText;
    [SerializeField] private Image player1_avtarImag;
    [SerializeField] private Image player1_timerImg;

    [Header("Player2 profile")]
    [SerializeField] private TextMeshProUGUI player2_nameText;
    [SerializeField] private Image player2_avtarImag;
    [SerializeField] private Image player2_timerImg;

    [Header("Game Over screens")]
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private Transform winScreenCoinImg;

    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI gameOverMsgText;

    [Header("Msg screens")]
    [SerializeField] private GameObject msgScreen;
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject msgHomeButton;
    [SerializeField] private GameObject loadingBar;

    [Space(15)]
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private GameObject coinDisplay;
    [SerializeField] private GameObject exitScreen;
    [SerializeField] private GameObject rematchScreen;
    [SerializeField] private EventManager eventManager;
    [SerializeField] private GameObject upperStrip;

    public Image GetTimerImg(int playerNumber)
    {
        return (playerNumber == 1) ? player1_timerImg : player2_timerImg;
    }

    public void ShowPlayerInfo(string player1_name, Sprite player1_Avtar, string player2_name, Sprite player2_Avtar)
    {
        player1_nameText.text = player1_name;
        player1_avtarImag.sprite = player1_Avtar;

        player2_nameText.text = player2_name;
        player2_avtarImag.sprite = player2_Avtar;
    }

    public void DisableAllScreen()
    {
        coinDisplay.SetActive(false);
        faderScreen.SetActive(false);

        winScreen.SetActive(false);
        loseScreen.SetActive(false);
        gameOverScreen.SetActive(false);

        exitScreen.SetActive(false);
        rematchScreen.SetActive(false);
        msgScreen.SetActive(false);
    }

    public void ToggleGameWinScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        //winScreen.SetActive(status);

        if (status)
        {
            winScreen.Activate();
            CoinManager.Instance.AddCoin(500, winScreenCoinImg);
        }
        else
        {
            winScreen.Deactivate();
        }
    }

    public void ToggleGameLoseScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        //loseScreen.SetActive(status);

        if (status)
        {
            loseScreen.Activate();
        }
        else
        {
            loseScreen.Deactivate();
        }
    }

    public void ToggleGameOverScreen(bool status, string winnerName = "", string loserName = "")
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        //gameOverScreen.SetActive(status);

        if (status)
        {
            gameOverScreen.Activate();
            gameOverMsgText.text = "The " + winnerName + " piece wins the game.Better luck next time, "+ loserName + " piece!";
        }
        else
        {
            gameOverScreen.Deactivate();
        }
    }

    public void ToggleExitScreen(bool status)
    {
        faderScreen.SetActive(status);

        if(status)
        {
            exitScreen.Activate();
        }
        else
        {
            exitScreen.Deactivate();
        }
    }

    public void ToggleRematchScreen(bool status)
    {
        faderScreen.SetActive(status);

        if (status)
        {
            rematchScreen.Activate();
        }
        else
        {
            rematchScreen.Deactivate();
        }
    }

    public void ToggleMsgScreen(bool status, string msg = "", bool homeButtonStatus = false)
    {
        faderScreen.SetActive(status);
        msgScreen.SetActive(status);
        msgText.text = msg;
        msgHomeButton.SetActive(homeButtonStatus);
        loadingBar.SetActive(!homeButtonStatus);
    }

    public void OnRematchButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        if(CoinManager.Instance.GetCoinAmount() >= 250)
        {
            if (GameManager.Instance.GameMode == GameMode.Online)
            {
                DisableAllScreen();
                ToggleMsgScreen(true, "waiting for opponent confirmation");
                eventManager.SendRematchConfirmationEvent();
            }
            else
            {
                DisableAllScreen();
                StartCoroutine(GameManager.Instance.Rematch());
            }
        }
        else
        {
            PersistentUI.Instance.shopScreen.gameObject.SetActive(true);
        }
    }

    public void OnRematchYesButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();

        if (CoinManager.Instance.GetCoinAmount() >= 250)
        {
            DisableAllScreen();

            eventManager.SendRematchAcceptEvent();

            ToggleMsgScreen(true, "waiting for match restart");
        }
    }

    public void OnRematchNoButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        DisableAllScreen();

        eventManager.SendRematchDeniedEvent();

        OnExitScreenYesButtonClick();
    }

    public bool CanOpenGameOverScreen()
    {
        return !(winScreen.activeSelf || loseScreen.activeSelf || msgScreen.activeSelf);
    }

    public void OnExitScreenYesButtonClick()
    {
        AudioManager.Instance.StopTimeTickingSound();
        if (GameManager.Instance.GameMode == GameMode.Online)
        {
            if (PhotonNetwork.IsConnected)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.DestroyAll();
                }

                PhotonNetwork.AutomaticallySyncScene = false;
                PhotonNetwork.LeaveRoom();
                ToggleExitScreen(false);
                AudioManager.Instance.PlayButtonClickSound();
            }
        }
        else
        {
            AudioManager.Instance.PlayButtonClickSound();
            SceneManager.LoadScene(0);
        }
    }

    public void OnPauseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleExitScreen(true);
    }

    public void OnExitScreenCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        ToggleExitScreen(false);
    }
}


