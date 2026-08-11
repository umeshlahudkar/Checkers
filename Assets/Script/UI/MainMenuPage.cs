using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPage : Page
{
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color unSelectedColor;
    [SerializeField] private BottomTile[] bottomTiles;

    private void Awake()
    {
        InitializeBottomTile();
        ServiceLocator.Get<AudioManager>().PlayBackgroundMusic();
    }

    private void InitializeBottomTile()
    {
        for(int i = 0; i < bottomTiles.Length; i++)
        {
            bool isSelected = bottomTiles[i].pageType == MenuPageType.MainMenuPage;
            bottomTiles[i].icon.color = isSelected ? selectedColor : unSelectedColor;
            bottomTiles[i].tileName.color = isSelected ? selectedColor : unSelectedColor;
            bottomTiles[i].selectorObj.SetActive(isSelected);
        }
    }

    public void OnPlayButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.ModeSelectionPage);
    }

    public void OnSettingButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().OpenPage(MenuPageType.Setting);
    }

    public void OnBottomDrawerButtonClick(int id)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if((MenuPageType)id == MenuPageType.MainMenuPage) { return; }
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
