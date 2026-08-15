using System;
using TMPro;
using UnityEngine;

// Generic Accept/Decline overlay popup - set header/description/button labels and the two callbacks
// at runtime via Show(...), then ServiceLocator.Get<DDOLPageManager>().OpenPageAsOverlay(DDOLPageType.
// ConfirmationPopup). Lives under DDOL_Canvas (see DDOLPageManager, alongside FaderPage/LoadingPage)
// rather than GamePageManager's per-scene page set, so it's reachable from any scene - main menu,
// matchmaking, gameplay - not just mid-match. Replaces what used to be three separate pages
// (DrawOfferPage/RematchOfferPage, then QuitPage) - see GameManager.ReceiveDrawOffer/
// ReceiveRematchOffer and GamePage.OnHomeButtonClick/OnRestartButtonClick for the current callers.
// Any future yes/no confirmation of the same shape reuses this instead of a new page/prefab.
//
// The popup closes itself on either button before invoking the callback, so callers only need to
// supply what should happen on accept/decline, not the page-closing itself.
public class ConfirmationPopup : Page
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI acceptButtonText;
    [SerializeField] private TextMeshProUGUI declineButtonText;

    private Action onAccept;
    private Action onDecline;

    public void Show(string header, string description, string acceptLabel, string declineLabel, Action onAccept, Action onDecline)
    {
        headerText.text = header;
        descriptionText.text = description;
        acceptButtonText.text = acceptLabel;
        declineButtonText.text = declineLabel;
        this.onAccept = onAccept;
        this.onDecline = onDecline;
    }

    public void OnAcceptButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<DDOLPageManager>().CloseOverlay(DDOLPageType.ConfirmationPopup);
        onAccept?.Invoke();
    }

    public void OnDeclineButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<DDOLPageManager>().CloseOverlay(DDOLPageType.ConfirmationPopup);
        onDecline?.Invoke();
    }
}
