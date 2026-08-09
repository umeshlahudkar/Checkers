using TMPro;
using UnityEngine;

// Overlay popup shown while a mutual-agreement draw offer is outstanding (see
// GameManager.OfferDraw/ReceiveDrawOffer). Follows the same Show(...)-then-OpenPageAsOverlay(...)
// pattern as VictoryPage/DefeatPage/DrawPage/RuleSetInfoPage.
//
// GameManager.ReceiveDrawOffer only ever opens this page for the client(s) actually meant to respond
// - in Multiplayer the offerer's own client is shown a "waiting for a response" floating text
// instead and never opens this page at all, so Show() doesn't need to distinguish that case itself.
public class DrawOfferPage : Page
{
    [SerializeField] private TextMeshProUGUI messageText;

    private int offeringPlayerNumber;

    public void Show(int offeringPlayerNumber)
    {
        this.offeringPlayerNumber = offeringPlayerNumber;
        messageText.text = "Your opponent offers a draw. Accept?";
    }

    public void OnAcceptButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GameManager>().RespondToDrawOffer(offeringPlayerNumber, true);
        ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.DrawOfferPage);
    }

    public void OnDeclineButtonClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        ServiceLocator.Get<GameManager>().RespondToDrawOffer(offeringPlayerNumber, false);
        ServiceLocator.Get<GamePageManager>().CloseOverlay(GamePageType.DrawOfferPage);
    }
}
