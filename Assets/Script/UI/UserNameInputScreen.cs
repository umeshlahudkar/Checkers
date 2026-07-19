using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserNameInputScreen : Page
{
    [SerializeField] private TMP_InputField usernameInputField;

    public void OnSaveButtonClick()
    {
        string username = usernameInputField.text;
        if (!string.IsNullOrEmpty(username))
        {
            ServiceLocator.Get<ProfileManager>().SetUserName(username);
            ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
            ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();

            //ServiceLocator.Get<MatchmakingConnectionManager>().SetProfile();
        }
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    protected override void OnClosed()
    {
        usernameInputField.text = string.Empty;
    }
}
