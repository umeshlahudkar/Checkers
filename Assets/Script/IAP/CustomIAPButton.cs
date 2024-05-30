using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CustomIAPButton : MonoBehaviour
{
    [SerializeField] private IAPProductID productID;
    [SerializeField] private Button button;
    [SerializeField] private Text priceText;

    private Product thisProduct = null;

    private void OnEnable()
    {
        InitIAPButton();
    }

    private void InitIAPButton()
    {
        if (thisProduct == null)
        {
            thisProduct = IAPManager.Instance.GetStoreProductByID(productID.ToString());
            priceText.text = thisProduct.metadata.localizedPrice.ToString();
            button.interactable = true;
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void OnButtonClick()
    {
        if(thisProduct != null)
        {
            IAPManager.Instance.PurchaseProduct(thisProduct);
        }
    }
}
