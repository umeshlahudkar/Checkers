using UnityEngine;

public class GameplayPageManager : PageManager<GameplayPageManager, GameplayPageType>
{
    [SerializeField] private GameObject coinDisplay;

    protected override void OnPageOpened(GameplayPageType key)
    {
        coinDisplay.SetActive(key != GameplayPageType.Exit);
    }

    protected override void OnPageClosed()
    {
        coinDisplay.SetActive(false);
    }
}
