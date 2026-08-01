using UnityEngine;

[System.Serializable]
public class ModeInfo
{
    public bool isActive = true;
    public GameModeType mode;
    public string modeName;
    [TextArea] public string modeDescription;
    public Sprite modeIcon;
    public bool showOnlineBadge;
}
