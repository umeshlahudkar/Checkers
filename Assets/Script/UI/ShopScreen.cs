using UnityEngine;
using UnityEngine.UI;

public class ShopScreen : Page
{
    [Header("Shop screen")]
    [SerializeField] private Button getCoinButton;
    [SerializeField] private Transform coinImg;

    [Header("Shop screen")]
    [SerializeField] private GameObject faderScreen;

    protected override void OnOpened()
    {
        faderScreen.SetActive(true);
    }

    protected override void OnClosed()
    {
        faderScreen.SetActive(false);
        getCoinButton.interactable = true;
    }

    public void OnGetCoinButtonClick(int coinAmount)
    {
        AudioManager.Instance.PlayButtonClickSound();
        getCoinButton.interactable = false;
        CoinManager.Instance.AddCoin(coinAmount, coinImg, ()=>
        {
            Close();
        });
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        Close();
    }
}
