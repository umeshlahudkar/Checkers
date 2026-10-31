using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// The short code a host shares for a private room. It is also the Photon room name. The alphabet
// leaves out the characters people misread when typing a code from a message (0/O, 1/I), so a code
// read aloud or off a screenshot comes out right the first time.
public static class RoomCode
{
    public const int Length = 5;

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        StringBuilder code = new StringBuilder(Length);
        for (int i = 0; i < Length; i++)
        {
            code.Append(Alphabet[UnityEngine.Random.Range(0, Alphabet.Length)]);
        }

        return code.ToString();
    }

    // Whether a typed character could be part of a code, in either case. Used to refuse the rest at
    // the keyboard (see JoinRoomPage) - cheaper and gentler than rewriting the field afterwards.
    public static bool IsAllowed(char c)
    {
        return Alphabet.IndexOf(char.ToUpperInvariant(c)) >= 0;
    }

    // Uppercases and drops anything that can't be part of a code, then trims to Length. Used both
    // for typed input and for pasted text, which may carry spaces, a trailing newline, or lowercase.
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        StringBuilder code = new StringBuilder(Length);
        foreach (char c in raw.ToUpperInvariant())
        {
            if (Alphabet.IndexOf(c) >= 0)
            {
                code.Append(c);
                if (code.Length == Length) { break; }
            }
        }

        return code.ToString();
    }

    // Finds the code in whatever was copied. A friend may copy just the code, a link, or the whole
    // invite message, and each has to give the same answer. Returns an empty string when there is no
    // code in the text. Tried in order: (1) the text is short enough to be just a code, (2) a room
    // link in it, (3) the word "code" followed by a code, (4) a standalone uppercase code.
    public static string Extract(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string trimmed = text.Trim();
        if (trimmed.Length <= Length * 2)
        {
            string direct = Sanitize(trimmed);
            return IsComplete(direct) ? direct : string.Empty;
        }

        foreach (string token in trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Contains("://") && DeepLinkConfig.TryParseRoomCode(token, out string linkCode))
            {
                return linkCode;
            }
        }

        Match labelled = Regex.Match(trimmed, @"\bcode\s*:?\s*([A-Za-z0-9]{5})\b", RegexOptions.IgnoreCase);
        if (labelled.Success)
        {
            string candidate = Sanitize(labelled.Groups[1].Value);
            if (IsComplete(candidate))
            {
                return candidate;
            }
        }

        Match standalone = Regex.Match(trimmed, @"\b[A-HJ-NP-Z2-9]{5}\b");
        return standalone.Success ? standalone.Value : string.Empty;
    }

    public static bool IsComplete(string code)
    {
        return code != null && code.Length == Length;
    }
}
