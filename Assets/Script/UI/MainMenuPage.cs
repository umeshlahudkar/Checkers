using UnityEngine;

public class MainMenuPage : Page
{
    public void OnPlayButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.ModeSelection);
    }

    public void OnProfileClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.UserNameInput);
    }

    public void OnSettingButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.OpenPage(MenuPageType.Setting);
    }

    public void OnHowToPlayButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
    }
}
