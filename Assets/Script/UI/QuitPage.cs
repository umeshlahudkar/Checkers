using System;
using UnityEngine;

public class QuitPage : Page
{
    public void OnQuitAnywayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<SceneLoader>().LoadScene("MainScene");
    }

    public void OnKeepPlayingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.QuitPage);
    }
}
