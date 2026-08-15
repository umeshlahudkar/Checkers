public class DDOLPageManager : PageManager<DDOLPageManager, DDOLPageType>
{
    public ConfirmationPopup ConfirmationPopup => GetPage(DDOLPageType.ConfirmationPopup) as ConfirmationPopup;
}
