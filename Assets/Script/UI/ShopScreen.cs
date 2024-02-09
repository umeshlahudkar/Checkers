using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

        StartCoroutine(DisableScreenAfterDelay());
    }

    private IEnumerator  DisableScreenAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        faderScreen.SetActive(false);
        gameObject.Deactivate();
        getCoinButton.interactable = true;
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        faderScreen.SetActive(false);
        gameObject.Deactivate();
        getCoinButton.interactable = true;
    }
}
