using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserNameInputScreen : MonoBehaviour
{
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private TMP_InputField usernameInputField;

    [SerializeField] private LobbyUIController lobbyUIController;

    public void OnSaveButtonClick()
    {
        string username = usernameInputField.text;
        if (!string.IsNullOrEmpty(username))
        {
            ProfileManager.Instance.SetUserName(username);
            AudioManager.Instance.PlayButtonClickSound();
            gameObject.SetActive(false);
            faderScreen.SetActive(false);

            lobbyUIController.SetProfile();
        }
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.SetActive(false);
    }
}
