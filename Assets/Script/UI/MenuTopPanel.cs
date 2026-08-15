using UnityEngine;

public class MenuTopPanel : Page
{
    public void OnSettingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Setting);
    }

    public void OnProfileButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.ProfilePage);
    }
}
