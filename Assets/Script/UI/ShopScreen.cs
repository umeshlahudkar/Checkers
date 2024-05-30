using UnityEngine;
using UnityEngine.UI;

public class ShopScreen : MonoBehaviour
{
    [SerializeField] private PurchaseMessageScreen purchaseMessageScreen;

    private void OnEnable()
    {
        IAPManager.Instance.OnIAPPurchaseSuccesful += OnPurchaseSuccesful;
        IAPManager.Instance.OnIAPPurchaseFailed += OnPurchaseFailed;
    }

    private void OnPurchaseSuccesful(IAPProduct product)
    {
        purchaseMessageScreen.InitScreen("Purchase Succesful", "Congratulation! You received " + product.reward + " Coins");
        CoinManager.Instance.AddCoin(product.reward, purchaseMessageScreen.coinImg);
    }

    private void OnPurchaseFailed(IAPProduct product, string massage) 
    {
        purchaseMessageScreen.InitScreen("Purchase Failed", massage);
    }

    private void OnDisable()
    {
        IAPManager.Instance.OnIAPPurchaseSuccesful -= OnPurchaseSuccesful;
        IAPManager.Instance.OnIAPPurchaseFailed -= OnPurchaseFailed;
    }
}
