using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePage : Page
{
    [SerializeField] private Image profileIcon;
    [SerializeField] private TMP_InputField userNameInputField;

    [SerializeField] private AvatarTile tileTemplate;
    [SerializeField] private Transform tileContainer;

    private readonly List<AvatarTile> avatarTiles = new();

    private bool tilesCreated;
    private int selectedAvtarIndex = -1;

    protected override void OnOpened()
    {
        CreateTiles();

        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        selectedAvtarIndex = profileManager.GetProfileAvtarID();
        HighlightSelectedAvatar(selectedAvtarIndex);

        if (selectedAvtarIndex > 0)
        {
            profileIcon.sprite = profileManager.GetProfileAvtar();
        }

        string userName = profileManager.GetUserName();
        if (!string.IsNullOrEmpty(userName))
        {
            userNameInputField.text = userName;
        }
    }

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        int avtarCount = ServiceLocator.Get<ProfileManager>().AvtarCount;
        for (int i = 1; i <= avtarCount; i++)
        {
            AvatarTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(i, ServiceLocator.Get<ProfileManager>().GetAvtar(i), OnAvatarSelected);
            avatarTiles.Add(tile);
        }

        tilesCreated = true;
    }

    private void OnAvatarSelected(int index)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        HighlightSelectedAvatar(index);
        profileIcon.sprite = ServiceLocator.Get<ProfileManager>().GetAvtar(index);
    }

    private void HighlightSelectedAvatar(int index)
    {
        if (selectedAvtarIndex > 0)
        {
            avatarTiles[selectedAvtarIndex - 1].SetSelected(false);
        }

        selectedAvtarIndex = index;

        if (selectedAvtarIndex > 0)
        {
            avatarTiles[selectedAvtarIndex - 1].SetSelected(true);
        }
    }

    public void OnSaveButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        if (selectedAvtarIndex > 0)
        {
            ServiceLocator.Get<ProfileManager>().SetAvtar(selectedAvtarIndex);
        }

        string enteredName = userNameInputField.text.Trim();
        if (!string.IsNullOrEmpty(enteredName))
        {
            ServiceLocator.Get<ProfileManager>().SetUserName(enteredName);
        }

        ServiceLocator.Get<MenuPageManager>().GoBack();
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
