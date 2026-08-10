using UnityEngine;

public class MainMenuPage : Page
{
    private void Start()
    {
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    public void OnPlayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.ModeSelectionPage);
    }

    public void OnProfileClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.ProfilePage);
    }

    public void OnSettingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Setting);
    }

    public void OnHowToPlayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
    }

    public void OnQuitButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        Application.Quit();
    }

    public void OnAvtarButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.AvtarSelection);
    }
}
