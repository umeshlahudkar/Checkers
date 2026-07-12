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

        selectedAvtarIndex = ServiceLocator.Get<ProfileManager>().GetProfileAvtarIndex();
        HighlightSelectedAvatar(selectedAvtarIndex);

        if (ServiceLocator.Get<ProfileManager>().HasAvtarSet)
        {
            profileIcon.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        }

        if (ServiceLocator.Get<ProfileManager>().HasUserNameSet)
        {
            userNameInputField.text = ServiceLocator.Get<ProfileManager>().GetUserName();
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
