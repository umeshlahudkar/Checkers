using UnityEngine;

[System.Serializable]
public class ModeInfo
{
    public bool isActive = true;
    public GameModeType mode;
    public string modeName;
    [TextArea] public string modeDescription;

    // Text for the tile's footer strip, for the modes whose footer is a plain label rather than a
    // control ("SAME DEVICE", "15S TURNS"). Left empty for VsBot and PrivateRoom, whose footers are
    // the difficulty buttons and the Create/Join pair instead - see ModeTile.Setup.
    public string footerLabel;

    public Sprite modeIcon;
    public bool showOnlineBadge;
    public bool enableTurnTimer = true;
}
