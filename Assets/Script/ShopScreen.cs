using UnityEngine;
using UnityEngine.UI;

public class ShopScreen : MonoBehaviour
{
    [Header("Shop screen")]
    [SerializeField] private Button getCoinButton;
    [SerializeField] private Transform coinImg;

    [Header("Shop screen")]
    [SerializeField] private GameObject faderScreen;


    private void OnEnable()
    {
        faderScreen.SetActive(true);
    }

    public void OnGetCoinButtonClick(int coinAmount)
    {
        AudioManager.Instance.PlayButtonClickSound();
        getCoinButton.interactable = false;
        CoinManager.Instance.AddCoin(coinAmount, coinImg);
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.SetActive(false);
        getCoinButton.interactable = true;
    }
}
