using UnityEngine;

public class MainMenuPage : Page
{
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
}
