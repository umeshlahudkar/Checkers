using UnityEngine;

public class ModeSelectionPage : Page
{
    [SerializeField] private ModeListSO modeList;
    [SerializeField] private ModeTile tileTemplate;
    [SerializeField] private Transform tileContainer;

    private bool tilesCreated;

    protected override void OnOpened()
    {
        CreateTiles();
    }

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        foreach (ModeInfo info in modeList.modes)
        {
            if (!info.isActive)
            {
                continue;
            }

            ModeTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(info, OnModeSelected);
        }

        tilesCreated = true;
    }

    private void OnModeSelected(ModeInfo info)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<PhotonNetworkManager>().StartMatch(info.mode);
    }

    public void OnCloseButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().CloseCurrentPage();
    }

    public void OnBackButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<MenuPageManager>().GoBack();
    }
}
