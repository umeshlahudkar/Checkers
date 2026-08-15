using UnityEngine;

public class MainMenuPage : Page
{
    private void Awake()
    {
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    private void Start()
    {
        // MainMenuPage starts active in the scene rather than being opened via
        // MenuPageManager, so OnOpened() (which shows the shared top/bottom bars)
        // would otherwise never fire on first launch.
        if (ServiceLocator.Get<MenuPageManager>().CurrentActivePage == null)
        {
            ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.MainMenuPage);
        }
    }

    protected override void OnOpened()
    {
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuTopPanel);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuBottomPanel);
    }

    public void OnPlayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.ModeSelectionPage);
    }
}
