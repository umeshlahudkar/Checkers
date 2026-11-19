using UnityEngine;
using TMPro;

public class LoadingScreen : Page
{
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private GameObject Bg;
    [SerializeField] private TextMeshProUGUI msgText;

    public void ActivateLoadingScreen(string msgToShow = "Loading...")
    {
        msgText.text = msgToShow;
        Open();
    }

    public void DeactivateLoadingScreen()
    {
        Close();
    }

    protected override void OnOpened()
    {
        faderScreen.SetActive(true);
        Bg.SetActive(true);
    }

    protected override void OnClosed()
    {
        faderScreen.SetActive(false);
        Bg.SetActive(false);
    }
}
