using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserNameInputScreen : Page
{
    [SerializeField] private TMP_InputField usernameInputField;

    [SerializeField] private LobbyUIController lobbyUIController;

    public void OnSaveButtonClick()
    {
        string username = usernameInputField.text;
        if (!string.IsNullOrEmpty(username))
        {
            ProfileManager.Instance.SetUserName(username);
            AudioManager.Instance.PlayButtonClickSound();
            MenuPageManager.Instance.CloseCurrentPage();

            lobbyUIController.SetProfile();
        }
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.CloseCurrentPage();
    }

    protected override void OnClosed()
    {
        usernameInputField.text = string.Empty;
    }
}
