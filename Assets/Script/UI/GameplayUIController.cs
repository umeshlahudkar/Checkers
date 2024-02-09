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

    [Header("Color")]
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color highlightColor;

    private float animationDuration = 1f;
    private float startTime = 0;
    private bool animFlag = false;
    private float maxAlpha = 1f;
    private float minAlpha = 0.5f;

    private int playerTurn = -1;
    private bool canPlayAnim = false;

    public Image GetTimerImg(int playerNumber)
    {
        return (playerNumber == 1) ? player1_timerImg : player2_timerImg;
    }

    public void ShowPlayerInfo(PlayerInfo player1_info, PlayerInfo player2_info)
    {
        player1_nameText.text = player1_info.userName;
        player1_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player1_info.avtarIndex);

        player2_nameText.text = player2_info.userName;
        player2_avtarImag.sprite = ProfileManager.Instance.GetAvtar(player2_info.avtarIndex);
    }

    public void DisablePlayerInfo()
    {
        upperStrip.SetActive(false);
    }

    public void PlayHighlightAnimation(int turn)
    {
        StopPlayerHighlightAnim();
        playerTurn = turn;
        canPlayAnim = true;
    }

    public void StopPlayerHighlightAnim()
    {
        canPlayAnim = false;
        startTime = 0;
        animFlag = false;
        playerTurn = -1;
        player1_timerImg.color = defaultColor;
        player2_timerImg.color = defaultColor;
    }

    private void Update()
    {
        if(canPlayAnim)
        {
            if (playerTurn == 1)
            {
                PlayPlayerTurnHighlightAnimation(player1_timerImg);
            }
            else if (playerTurn == 2)
            {
                PlayPlayerTurnHighlightAnimation(player2_timerImg);
            }
        }
    }

    private void PlayPlayerTurnHighlightAnimation(Image highlightImg)
    {
        float t = (Time.time - startTime) / animationDuration;
        float newAlpha;

        if (animFlag)
        {
            newAlpha = Mathf.Lerp(maxAlpha, minAlpha, t);
        }
        else
        {
            newAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);
        }

        SetHighlightImageAlpha(highlightImg, newAlpha);

        if (t >= animationDuration)
        {
            animFlag = !animFlag;
            startTime = Time.time;
        }
    }

    private void SetHighlightImageAlpha(Image highlightImg, float newAlpha)
    {
        Color currentColor = highlightColor;
        Color newColor = new(currentColor.r, currentColor.g, currentColor.b, newAlpha);
        highlightImg.color = newColor;
    }

    public void DisableAllScreen()
    {
        coinDisplay.SetActive(false);
        faderScreen.SetActive(false);

        winScreen.SetActive(false);
        loseScreen.SetActive(false);

        exitScreen.SetActive(false);
        rematchScreen.SetActive(false);
        msgScreen.SetActive(false);
    }

    public void ToggleGameWinScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        winScreen.SetActive(status);

        if (status)
        {
            CoinManager.Instance.AddCoin(500, winScreenCoinImg);
        }
    }

    public void ToggleGameLoseScreen(bool status)
    {
        coinDisplay.SetActive(status);
        faderScreen.SetActive(status);
        loseScreen.SetActive(status);
    }

    public void ToggleExitScreen(bool status)
    {
        faderScreen.SetActive(status);
        exitScreen.SetActive(status);
    }

    public void ToggleRematchScreen(bool status)
    {
        faderScreen.SetActive(status);
        rematchScreen.SetActive(status);
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
        if(GameManager.Instance.GameMode == GameMode.Online)
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


