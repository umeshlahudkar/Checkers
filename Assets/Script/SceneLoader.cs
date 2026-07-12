using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Service<SceneLoader>
{
    public void LoadScene(string sceneName, Action onComplete = null)
    {
        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
    {
        DDOLPageManager ddolPageManager = ServiceLocator.Get<DDOLPageManager>();
        ddolPageManager.OpenPage(DDOLPageType.LoadingPage);
        FaderPage faderPage = (FaderPage)ddolPageManager.GetPage(DDOLPageType.LoadingPage);

        yield return faderPage.FadeIn();

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return faderPage.FadeOut();

        ddolPageManager.GoBack();

        onComplete?.Invoke();
    }
}
