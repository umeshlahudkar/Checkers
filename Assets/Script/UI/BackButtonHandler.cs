using UnityEngine;

// Routes the device back button (Android reports it as Escape) to whichever screen is showing.
// Lives on the DDOL canvas next to DDOLPageManager so it keeps running across scene loads.
//
// Order matters: a scene change in progress swallows the press, then a confirmation popup (which
// sits above everything and always treats back as its "no" answer), and only then the current
// scene's own pages - the menu's or the match's. Each page decides what back means for it, see
// Page.OnBackPressed.
public class BackButtonHandler : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBack();
        }
    }

    public void HandleBack()
    {
        // Same scope as the old pages' buttons: nothing here works while the loading screen is up.
        if (!ServiceLocator.TryGet(out DDOLPageManager ddolPageManager)) { return; }
        if (ddolPageManager.CurrentActivePage != null) { return; }

        if (ddolPageManager.IsPageOpen(DDOLPageType.ConfirmationPopup))
        {
            ddolPageManager.HandleBack();
            return;
        }

        if (ServiceLocator.TryGet(out MenuPageManager menuPageManager))
        {
            menuPageManager.HandleBack();
        }
        else if (ServiceLocator.TryGet(out GamePageManager gamePageManager))
        {
            gamePageManager.HandleBack();
        }
    }
}
