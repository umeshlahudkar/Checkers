using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using static UnityEditor.ObjectChangeEventStream;

public class IAPManager : Singleton<IAPManager>, IDetailedStoreListener
{
    private IStoreController controller;
    private IExtensionProvider extensions;

    [SerializeField] private IAPProductSO iapProductSO;

    public event Action<IAPProduct> OnIAPPurchaseSuccesful;
    public event Action<IAPProduct, string> OnIAPPurchaseFailed;

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
        if(IsIAPInitialized())
        {
            HandlePurchaseSuccesful(e.purchasedProduct);
        }
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
        if (IsIAPInitialized())
        {
            HandlePurchaseFailed(i, p.message);
        }
    }

    public bool IsIAPInitialized()
    {
        return (controller != null && extensions != null);
    }

    private void HandlePurchaseSuccesful(Product purchasedProduct)
    {
        if(OnIAPPurchaseSuccesful != null)
        {
            IAPProductID id = GetIAPProductID(purchasedProduct.definition.id);
            IAPProduct iapPurchasedProduct = GetIAPProductByID(id);

            OnIAPPurchaseSuccesful.Invoke(iapPurchasedProduct);
        }
    }

    private void HandlePurchaseFailed(Product purchasedProduct, string message)
    {
        if (OnIAPPurchaseFailed != null)
        {
            IAPProductID id = GetIAPProductID(purchasedProduct.definition.id);
            IAPProduct iapPurchasedProduct = GetIAPProductByID(id);

            OnIAPPurchaseFailed.Invoke(iapPurchasedProduct, message);
        }
    }

    public Product GetStoreProductByID(string productID)
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

    public IAPProduct GetIAPProductByID(IAPProductID productID)
    {
        IAPProduct[] products = iapProductSO.iAPProducts;
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].productID == productID) 
            { 
                return products[i];
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

    private IAPProductID GetIAPProductID(string productID)
    {
        IAPProductID id = IAPProductID.None;
        switch(productID)
        {
            case "coins1000":
                id = IAPProductID.coins1000;
                break;

            case "coins25000":
                id = IAPProductID.coins25000;
                break;

            case "coins75000":
                id = IAPProductID.coins75000;
                break;

            case "coins150000":
                id = IAPProductID.coins150000;
                break;

            case "coins500000":
                id = IAPProductID.coins500000;
                break;

            case "coins200000":
                id = IAPProductID.coins200000;
                break;
        }

        return id;
    }
   
}
