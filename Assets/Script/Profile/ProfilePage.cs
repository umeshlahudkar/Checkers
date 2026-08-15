using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePage : Page
{
    // Username
    [SerializeField] private TMP_InputField userNameInputField;

    // Avatar
    [SerializeField] private AvatarTile tileTemplate;
    [SerializeField] private Transform tileContainer;
    private readonly List<AvatarTile> avatarTiles = new();
    private bool tilesCreated;
    private int selectedAvtarIndex = -1;

    // Piece
    [SerializeField] private AvatarTile whitePieceTile;
    [SerializeField] private AvatarTile blackPieceTile;
    private PieceType selectedPieceType = PieceType.None;

    // Shared by Avatar and Piece tiles
    [SerializeField] private Sprite selectedBg;
    [SerializeField] private Sprite unSelectedBg;

    protected override void OnOpened()
    {
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuTopPanel);
        ServiceLocator.Get<MenuPageManager>().OpenPageAsOverlay(MenuPageType.MenuBottomPanel);

        ProfileManager profileManager = ServiceLocator.Get<ProfileManager>();

        SetupUserName(profileManager);
        SetupAvatarTiles(profileManager);
        SetupPieceTiles(profileManager);
    }

    #region Username

    private void SetupUserName(ProfileManager profileManager)
    {
        string userName = profileManager.GetUserName();
        if (!string.IsNullOrEmpty(userName))
        {
            userNameInputField.text = userName;
        }
    }

    public void OnSaveButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();

        string enteredName = userNameInputField.text.Trim();
        if (!string.IsNullOrEmpty(enteredName))
        {
            ServiceLocator.Get<ProfileManager>().SetUserName(enteredName);
        }
    }

    #endregion

    #region Avatar

    private void SetupAvatarTiles(ProfileManager profileManager)
    {
        CreateTiles();

        selectedAvtarIndex = profileManager.GetProfileAvtarID();
        HighlightSelectedAvatar(selectedAvtarIndex);
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
            tile.Setup(i, ServiceLocator.Get<ProfileManager>().GetAvtar(i), unSelectedBg, OnAvatarSelected);
            avatarTiles.Add(tile);
        }

        tilesCreated = true;
    }

    private void OnAvatarSelected(int index)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        HighlightSelectedAvatar(index);
        ServiceLocator.Get<ProfileManager>().SetAvtar(index);
    }

    private void HighlightSelectedAvatar(int index)
    {
        if (selectedAvtarIndex > 0)
        {
            avatarTiles[selectedAvtarIndex - 1].SetSelected(unSelectedBg);
        }

        selectedAvtarIndex = index;

        if (selectedAvtarIndex > 0)
        {
            avatarTiles[selectedAvtarIndex - 1].SetSelected(selectedBg);
        }
    }

    #endregion

    #region Piece

    private void SetupPieceTiles(ProfileManager profileManager)
    {
        whitePieceTile.Setup((int)PieceType.White, profileManager.GetPieceAvtar(PieceType.White), unSelectedBg, OnPieceSelected);
        blackPieceTile.Setup((int)PieceType.Black, profileManager.GetPieceAvtar(PieceType.Black), unSelectedBg, OnPieceSelected);

        HighlightSelectedPiece((PieceType)profileManager.GetProfilePieceID());
    }

    private void OnPieceSelected(int index)
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        HighlightSelectedPiece((PieceType)index);
        ServiceLocator.Get<ProfileManager>().SetPiece(index);
    }

    private void HighlightSelectedPiece(PieceType pieceType)
    {
        GetPieceTile(selectedPieceType)?.SetSelected(unSelectedBg);
        selectedPieceType = pieceType;
        GetPieceTile(selectedPieceType)?.SetSelected(selectedBg);
    }

    private AvatarTile GetPieceTile(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.White => whitePieceTile,
            PieceType.Black => blackPieceTile,
            _ => null
        };
    }

    #endregion

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
