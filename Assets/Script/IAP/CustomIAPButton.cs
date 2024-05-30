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

    private bool isInitialized = false;
    private Product thisProduct = null;

    private void OnEnable()
    {
        if(!isInitialized)
        {
            thisProduct = IAPManager.Instance.GetStoreProductByID(productID.ToString());
            if(thisProduct != null )
            {
                priceText.text = thisProduct.metadata.localizedPrice.ToString();
                button.interactable = true;
                button.onClick.AddListener(OnButtonClick);
                isInitialized = true;
            }
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
