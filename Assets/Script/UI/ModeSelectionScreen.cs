using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class ModeSelectionScreen : MonoBehaviour
{
    [SerializeField] private LobbyUIController lobbyUIController;
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private Image[] buttons;
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
                    break;

                case GameMode.PVP:
                case GameMode.PVC:
                    SceneManager.LoadScene(1);
                    break;
            }

            faderScreen.SetActive(false);
            gameObject.SetActive(false);
        }
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
