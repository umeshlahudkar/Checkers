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
            ModeTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(info, OnModeSelected);
        }

        tilesCreated = true;
    }

    private void OnModeSelected(ModeInfo info)
    {
        AudioManager.Instance.PlayButtonClickSound();
        LobbyManager.Instance.StartMatch(info.mode);
    }

    public void OnCloseButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.CloseCurrentPage();
    }

    public void OnBackButtonClick()
    {
        AudioManager.Instance.PlayButtonClickSound();
        MenuPageManager.Instance.GoBack();
    }
}
