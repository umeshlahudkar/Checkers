using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField] private GameObject faderScreen;
    [SerializeField] private GameObject Bg;
    [SerializeField] private TextMeshProUGUI msgText;

    public void ActivateLoadingScreen(string msgToShow = "Loading...")
    {
        ToggleLoadingScreen(true);
        msgText.text = msgToShow;
    }

    public void DeactivateLoadingScreen()
    {
        ToggleLoadingScreen(false);
    }

    private void ToggleLoadingScreen(bool status)
    {
        gameObject.SetActive(status);
        faderScreen.SetActive(status);
        Bg.SetActive(status);
    }
}
