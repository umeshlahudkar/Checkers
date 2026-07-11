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

        selectedAvtarIndex = ProfileManager.Instance.GetProfileAvtarIndex();
        HighlightSelectedAvatar(selectedAvtarIndex);

        if (ProfileManager.Instance.HasAvtarSet)
        {
            profileIcon.sprite = ProfileManager.Instance.GetProfileAvtar();
        }

        if (ProfileManager.Instance.HasUserNameSet)
        {
            userNameInputField.text = ProfileManager.Instance.GetUserName();
        }
    }

    private void CreateTiles()
    {
        if (tilesCreated)
        {
            return;
        }

        int avtarCount = ProfileManager.Instance.AvtarCount;
        for (int i = 1; i <= avtarCount; i++)
        {
            AvatarTile tile = Instantiate(tileTemplate, tileContainer);
            tile.gameObject.SetActive(true);
            tile.Setup(i, ProfileManager.Instance.GetAvtar(i), OnAvatarSelected);
            avatarTiles.Add(tile);
        }

        tilesCreated = true;
    }

    private void OnAvatarSelected(int index)
    {
        AudioManager.Instance.PlayButtonClickSound();
        HighlightSelectedAvatar(index);
        profileIcon.sprite = ProfileManager.Instance.GetAvtar(index);
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
        AudioManager.Instance.PlayButtonClickSound();

        if (selectedAvtarIndex > 0)
        {
            ProfileManager.Instance.SetAvtar(selectedAvtarIndex);
        }

        string enteredName = userNameInputField.text.Trim();
        if (!string.IsNullOrEmpty(enteredName))
        {
            ProfileManager.Instance.SetUserName(enteredName);
        }

        MenuPageManager.Instance.GoBack();
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
