using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuBottomPanel : Page
{
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unSelectedColor;
    [SerializeField] private BottomTile[] bottomTiles;

    protected override void OnOpened()
    {
        InitializeBottomTile();
    }

    private void InitializeBottomTile()
    {
        MenuPageType currentPageType = ServiceLocator.Get<MenuPageManager>().CurrentPageKey;

        for (int i = 0; i < bottomTiles.Length; i++)
        {
            bool isSelected = bottomTiles[i].pageType == currentPageType;
            bottomTiles[i].icon.color = isSelected ? selectedColor : unSelectedColor;
            bottomTiles[i].tileName.color = isSelected ? selectedColor : unSelectedColor;
            bottomTiles[i].selectorObj.SetActive(isSelected);
        }
    }

    public void OnBottomDrawerButtonClick(int id)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if ((MenuPageType)id == ServiceLocator.Get<MenuPageManager>().CurrentPageKey) { return; }
        ServiceLocator.Get<MenuPageManager>().OpenPage((MenuPageType)id);
    }

    [System.Serializable]
    public class BottomTile
    {
        public MenuPageType pageType;
        public Image icon;
        public TextMeshProUGUI tileName;
        public GameObject selectorObj;
    }
}
