using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrawPage : Page
{
    [Header("Avatars")]
    [SerializeField] private Image localAvatarImage;
    [SerializeField] private Image opponentAvatarImage;

    [Header("Rematch (PrimaryButton's own Image - see CustomButton)")]
    [SerializeField] private Image rematchButtonImage;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private TextMeshProUGUI localPiecesText;
    [SerializeField] private TextMeshProUGUI opponentPiecesText;
    [SerializeField] private TextMeshProUGUI reasonText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI capturesText;
    [SerializeField] private TextMeshProUGUI kingsCrownedText;
    [SerializeField] private TextMeshProUGUI matchTimeText;

    public void Show(GameResult result)
    {
        localAvatarImage.sprite = ServiceLocator.Get<ProfileManager>().GetProfileAvtar();
        opponentAvatarImage.sprite = result.OpponentAvatar;
        opponentNameText.text = result.OpponentName;
        localPiecesText.text = result.LocalPiecesLeft.ToString();
        opponentPiecesText.text = result.OpponentPiecesLeft.ToString();
        reasonText.text = result.Reason;

        capturesText.text = $"{result.LocalCaptures} : {result.OpponentCaptures}";
        kingsCrownedText.text = $"{result.LocalKingsCrowned} : {result.OpponentKingsCrowned}";
        matchTimeText.text = result.MatchDuration;

        // Defensive reset - a previous match on this same page instance may have disabled the
        // button after a declined rematch offer (see SetRematchButtonInteractable).
        SetRematchButtonInteractable(true);
    }

    public void OnPlayAgainButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        GameManager gameManager = ServiceLocator.Get<GameManager>();
        if (gameManager.GameMode == GameModeType.Multiplayer)
        {
            gameManager.OfferRematch();
        }
        else
        {
            gameManager.StartRematch();
        }
    }

    // CustomButton (unlike a standard UI Button) has no built-in disabled state - gating the
    // button's own Image's raycast target is what actually stops IPointerDown/IPointerUp/OnClick
    // from ever reaching it (see CustomButton, and GamePage.SetOfferDrawButtonClickable for the same
    // trick applied to the Offer Draw button).
    public void SetRematchButtonInteractable(bool interactable)
    {
        rematchButtonImage.raycastTarget = interactable;
    }

    public void OnMainMenuButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<AudioManager>().StopTimeTickingSound();

        ServiceLocator.Get<GameManager>().GoToMainMenu();
    }

    public void OnShareButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
    }
}
