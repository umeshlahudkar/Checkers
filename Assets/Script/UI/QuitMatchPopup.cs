using System;
using UnityEngine;

public class QuitMatchPopup : MonoBehaviour
{
    private Action onQuitAnyway;
    private Action onKeepPlaying;

    public void Setup(Action onQuitAnywayClicked, Action onKeepPlayingClicked)
    {
        onQuitAnyway = onQuitAnywayClicked;
        onKeepPlaying = onKeepPlayingClicked;
    }

    public void OnQuitAnywayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        onQuitAnyway?.Invoke();
    }

    public void OnKeepPlayingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        onKeepPlaying?.Invoke();
    }
}
