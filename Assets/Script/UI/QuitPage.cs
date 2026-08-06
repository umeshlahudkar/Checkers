using Photon.Pun;

public class QuitPage : Page
{
    public void OnQuitAnywayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        PhotonNetwork.Disconnect();
        ServiceLocator.Get<SceneLoader>().LoadScene("MainScene");
    }

    public void OnKeepPlayingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.QuitPage);
    }

    public void OnRestartButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.QuitPage);
        ServiceLocator.Get<GameManager>().StartRematch();
    }
}
