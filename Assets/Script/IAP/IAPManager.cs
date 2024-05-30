using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : Singleton<IAPManager>, IDetailedStoreListener
{
    private IStoreController controller;
    private IExtensionProvider extensions;

    [SerializeField] private IAPProductSO iapProductSO;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        StandardPurchasingModule.Instance().useFakeStoreUIMode = FakeStoreUIMode.StandardUser;
        InitIAPProduct();
    }

    private void InitIAPProduct()
    {
        ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        IAPProduct[] products = iapProductSO.iAPProducts;
        for (int i = 0; i < products.Length; i++)
        {
            builder.AddProduct(products[i].productID.ToString(), ProductType.Consumable);
        }

        UnityPurchasing.Initialize(this, builder);
    }

    /// <summary>
    /// Called when Unity IAP is ready to make purchases.
    /// </summary>
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        this.controller = controller;
        this.extensions = extensions;

        Debug.Log("IAP initialization succesful");
    }

    /// <summary>
    /// Called when Unity IAP encounters an unrecoverable initialization error.
    ///
    /// Note that this will not be called if Internet is unavailable; Unity IAP
    /// will attempt initialization until it becomes available.
    /// </summary>
    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.Log("IAP intialization failed ");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.Log(message);
    }

    /// <summary>
    /// Called when a purchase completes.
    ///
    /// May be called at any time after OnInitialized().
    /// </summary>
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
    {
        return PurchaseProcessingResult.Complete;
    }

    /// <summary>
    /// Called when a purchase fails.
    /// IStoreListener.OnPurchaseFailed is deprecated,
    /// use IDetailedStoreListener.OnPurchaseFailed instead.
    /// </summary>
    public void OnPurchaseFailed(Product i, PurchaseFailureReason p)
    {
    }

    /// <summary>
    /// Called when a purchase fails.
    /// </summary>
    public void OnPurchaseFailed(Product i, PurchaseFailureDescription p)
    {

    }

    public bool IsIAPInitialized()
    {
        return (controller != null && extensions != null);
    }

    public Product GetProductByID(string productID)
    {
        if(IsIAPInitialized())
        {
            Product product = controller.products.WithID(productID);
            if(product != null && product.availableToPurchase) 
            {
                return product;
            }
        }

        return null;
    }

    public void PurchaseProduct(Product product)
    {
        if(IsIAPInitialized() && product != null)
        {
            controller.InitiatePurchase(product);
        }
    }
   
}
