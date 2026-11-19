using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
#endif

// Starts Firebase when the start scene loads, from the object under Managers. Only the default
// reporting is used: Analytics logs its automatic events (first_open, session_start, screen_view...)
// and Crashlytics reports crashes; nothing is logged by hand.
//
// Fire-and-forget on purpose: the splash doesn't wait for it, and the game keeps running if Firebase
// can't start (for example Google Play services missing).
//
// Also runs in the editor, so the log line shows the setup works. There the SDK uses desktop stubs
// (the config comes from StreamingAssets/google-services-desktop.json): nothing is sent to Firebase
// from Play mode, only builds on a device report.
public class FirebaseInitializer : MonoBehaviour
{
    // The start scene can be loaded again; Firebase only needs to be started once per launch.
    private static bool started;

    private void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (started)
        {
            return;
        }

        started = true;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"[Firebase] Dependency check failed: {task.Exception}");
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                // Typically Google Play services missing or out of date.
                Debug.LogError($"[Firebase] Not available: {task.Result}");
                return;
            }

            // Touching the default instance creates the app from google-services.json.
            _ = FirebaseApp.DefaultInstance;

            // Both are on by default; stated here so a disabled flag in a manifest can't silently turn them off.
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Crashlytics.IsCrashlyticsCollectionEnabled = true;

            Debug.Log("[Firebase] Initialized (Analytics and Crashlytics)");
        });
#endif
    }
}
