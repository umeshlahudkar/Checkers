using System;
using UnityEngine;

// Receives room links - the game was opened by one, or one arrived while it was running - and hands
// the room code to whoever can use it (the main menu, which opens the Join popup and joins).
//
// Lives on the always-present start-scene object so it is already listening when the game is
// launched cold by a link, which is before the menu exists. A code that arrives with nobody
// listening yet is kept briefly instead of dropped.
public class DeepLinkService : Service<DeepLinkService>
{
    // Long enough for a cold start to get through loading to the menu, short enough that a link
    // forgotten behind another screen can't join a room minutes later.
    private const float PendingLifetimeSeconds = 60f;

    // Raised the moment a code arrives, if anything is listening. The main menu listens while open.
    public event Action<string> RoomCodeReceived;

    private string pendingCode;
    private float pendingReceivedAt;
    private bool isSubscribed;

    protected override void Awake()
    {
        base.Awake();

        // Service<T> destroys a second copy (the start scene can be loaded again) - that copy must
        // not subscribe, or every link would be handled twice.
        if (!ServiceLocator.TryGet(out DeepLinkService current) || current != this)
        {
            return;
        }

        Application.deepLinkActivated += HandleUrl;
        isSubscribed = true;

        // A link that launched the game from closed isn't announced through deepLinkActivated - it is
        // only left in absoluteURL.
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            HandleUrl(Application.absoluteURL);
        }
    }

    protected override void OnDestroy()
    {
        if (isSubscribed)
        {
            Application.deepLinkActivated -= HandleUrl;
        }

        base.OnDestroy();
    }

    public void HandleUrl(string url)
    {
        if (!DeepLinkConfig.TryParseRoomCode(url, out string roomCode))
        {
            Debug.Log($"[DeepLinkService] Ignoring a link that is not a room link: {url}");
            return;
        }

        Debug.Log($"[DeepLinkService] Room link received, code {roomCode}");

        // Mid-match there is nothing sensible to do with it - leaving the match for a link would be a
        // surprise - so the player is told and the link is dropped.
        if (ServiceLocator.TryGet(out GameManager _))
        {
            ServiceLocator.Get<WarningNotifier>().Show("Finish this match, then open the link again.");
            return;
        }

        if (RoomCodeReceived != null)
        {
            RoomCodeReceived.Invoke(roomCode);
            return;
        }

        pendingCode = roomCode;
        pendingReceivedAt = Time.realtimeSinceStartup;
    }

    // Takes the code that arrived while nobody was listening, if it is still fresh.
    public bool TryTakePendingCode(out string roomCode)
    {
        roomCode = pendingCode;
        bool fresh = !string.IsNullOrEmpty(pendingCode)
            && Time.realtimeSinceStartup - pendingReceivedAt <= PendingLifetimeSeconds;

        pendingCode = null;
        if (!fresh)
        {
            roomCode = null;
        }

        return fresh;
    }
}
