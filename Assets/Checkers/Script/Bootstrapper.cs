using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string nextSceneName = GameConstants.Scenes.MainScene;
    [SerializeField] private float splashDuration = 1f;

    private const float SplashFadeDuration = 0.8f;

    // The first frames after the scene loads can take far longer than a normal frame, and a fade that
    // adds the real frame time would jump most of the way in that one frame. Each step is capped so
    // the fade always plays out visibly.
    private const float MaxFadeStep = 1f / 30f;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        // The services only read small local save files, so they run while the splash is on screen
        // instead of behind a loading page of their own.
        Coroutine initialization = StartCoroutine(InitializeServices());

        yield return StartCoroutine(HandleSplashPage());
        yield return initialization;

        // Fire-and-forget: SceneLoader lives on a DontDestroyOnLoad object, so it keeps
        // running the fade-out/GoBack() steps after this scene (and this Bootstrapper)
        // is unloaded - no need to yield on it from here.
        ServiceLocator.Get<SceneLoader>().LoadScene(nextSceneName);
    }

    private IEnumerator InitializeServices()
    {
        IInitializable[] services =
        {
            ServiceLocator.Get<GameSettingsManager>(),
            ServiceLocator.Get<ProfileManager>(),
            ServiceLocator.Get<AudioManager>(),
        };

        for (int i = 0; i < services.Length; i++)
        {
            yield return StartCoroutine(services[i].Initialize());
        }
    }

    private IEnumerator HandleSplashPage()
    {
        StartPageManager pageManager = ServiceLocator.Get<StartPageManager>();
        pageManager.OpenPage(StartScenePageType.SplashPage);

        SplashPage splashPage = (SplashPage)pageManager.GetPage(StartScenePageType.SplashPage);
        CanvasGroup canvasGroup = splashPage.canvasGroup;

        // Start fully transparent, let the first (slow) frames pass, then fade in.
        canvasGroup.alpha = 0f;
        yield return null;
        yield return null;
        yield return FadeCanvasGroup(canvasGroup, from: 0f, to: 1f, duration: SplashFadeDuration);

        yield return new WaitForSeconds(splashDuration);

        yield return FadeCanvasGroup(canvasGroup, from: 1f, to: 0f, duration: SplashFadeDuration);

        // One frame fully faded out before the splash closes and the next page opens.
        yield return null;
        pageManager.GoBack();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
