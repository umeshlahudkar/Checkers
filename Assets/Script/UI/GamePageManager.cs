public class GamePageManager : PageManager<GamePageManager, GamePageType>
{
    public GamePage GamePage => GetPage(GamePageType.GamePage) as GamePage;
}
