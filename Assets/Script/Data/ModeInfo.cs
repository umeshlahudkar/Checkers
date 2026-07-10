using UnityEngine;

[System.Serializable]
public class ModeInfo
{
    public GameMode mode;
    public string modeName;
    [TextArea] public string modeDescription;
    public Sprite modeIcon;
}
