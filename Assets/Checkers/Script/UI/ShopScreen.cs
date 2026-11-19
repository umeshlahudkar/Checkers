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
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        getCoinButton.interactable = false;
        //ServiceLocator.Get<CoinManager>().AddCoin(coinAmount, coinImg, ()=>
        //{
        //    Close();
        //});
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        Close();
    }
}
