using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] private string nextSceneName = "MainScene";
    [SerializeField] private float splashDuration = 1f;

    private void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        yield return StartCoroutine(HandleSplashPage());
        yield return StartCoroutine(HandleLoadingPage());
        yield return StartCoroutine(HandleSceneLoading());
    }

    private IEnumerator HandleSceneLoading() 
    {
        DDOLPageManager ddolPageManager = ServiceLocator.Get<DDOLPageManager>();

        ddolPageManager.OpenPage(DDOLPageType.LoadingPage);
        FaderPage faderPage = (FaderPage)ddolPageManager.GetPage(DDOLPageType.LoadingPage);

        yield return faderPage.FadeIn();
        yield return SceneManager.LoadSceneAsync(nextSceneName);
        yield return faderPage.FadeOut();

        ddolPageManager.GoBack();
    }

    private IEnumerator HandleLoadingPage()
    {
        StartPageManager pageManager = ServiceLocator.Get<StartPageManager>();
        pageManager.OpenPage(StartScenePageType.LoadingPage);
        LoadingPage loadingPage = (LoadingPage)pageManager.GetPage(StartScenePageType.LoadingPage);

        (string label, IInitializable service)[] steps =
        {
            ("Loading settings", ServiceLocator.Get<GameSettingsManager>()),
            ("Loading profile", ServiceLocator.Get<ProfileManager>()),
            ("Loading audio", ServiceLocator.Get<AudioManager>()),
        };

        for (int i = 0; i < steps.Length; i++)
        {
            loadingPage.SetProgress((float)i / steps.Length, steps[i].label + "...");
            yield return StartCoroutine(steps[i].service.Initialize());
            yield return new WaitForSeconds(0.25f);
        }

        loadingPage.SetProgress(1f, "Ready");
    }

    private IEnumerator HandleSplashPage()
    {
        StartPageManager pageManager = ServiceLocator.Get<StartPageManager>();
        pageManager.OpenPage(StartScenePageType.SplashPage);

        SplashPage splashPage = (SplashPage)pageManager.GetPage(StartScenePageType.SplashPage);
        CanvasGroup canvasGroup = splashPage.canvasGroup;

        // Start fully transparent, then fade in.
        yield return FadeCanvasGroup(canvasGroup, from: 0f, to: 1f, duration: 1f);

        yield return new WaitForSeconds(splashDuration);

        yield return FadeCanvasGroup(canvasGroup, from: 1f, to: 0f, duration: 1f);

        pageManager.GoBack();
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float from, float to, float duration)
    {
        canvasGroup.alpha = from;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
