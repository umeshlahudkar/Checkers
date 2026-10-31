using System;

// The one place that knows what a room link looks like: how it is built for the share message and how
// an incoming one is read back into a room code.
//
// The link is a custom-scheme one, "checkerking://join/K7P2X". For Android to open the game from it,
// the manifest needs a matching intent filter (scheme "checkerking", host "join") - that is added when
// the game is finalized, and OpenLinkEnabled is switched on at the same time. Until then the share
// message leaves the link out rather than send a link that does nothing.
public static class DeepLinkConfig
{
    public const string Scheme = "checkerking";

    private const string JoinPathWord = "join";

    // Flip to true together with adding the intent filter to the Android manifest.
    public static readonly bool OpenLinkEnabled = false;

    public static string BuildJoinLink(string roomCode)
    {
        return $"{Scheme}://{JoinPathWord}/{roomCode}";
    }

    // Reads a room code out of a link. Accepts the custom form ("checkerking://join/K7P2X") and the
    // same path on a web address ("https://example.com/anything/join/K7P2X"), so a web link can be
    // added later without touching this. The code has to be a complete, valid one.
    public static bool TryParseRoomCode(string url, out string roomCode)
    {
        roomCode = null;

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
        {
            return false;
        }

        bool isCustomScheme = string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase);
        bool isWebLink = uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
        if (!isCustomScheme && !isWebLink)
        {
            return false;
        }

        // In a custom-scheme link the word after "//" is parsed as the host, so it counts as the first
        // path token; in a web link the host is the website and doesn't.
        System.Collections.Generic.List<string> tokens = new System.Collections.Generic.List<string>();
        if (isCustomScheme)
        {
            tokens.Add(uri.Host);
        }

        foreach (string segment in uri.Segments)
        {
            string token = segment.Trim('/');
            if (token.Length > 0)
            {
                tokens.Add(token);
            }
        }

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (string.Equals(tokens[i], JoinPathWord, StringComparison.OrdinalIgnoreCase))
            {
                string candidate = RoomCode.Sanitize(tokens[i + 1]);
                if (RoomCode.IsComplete(candidate))
                {
                    roomCode = candidate;
                    return true;
                }
            }
        }

        return false;
    }
}
