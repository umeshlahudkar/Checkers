using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One mode button on the main menu: the gold Play Online card or one of the smaller mode cards. Which
// mode a card is for is set in the prefab; the page fills in the icon, name and description from the
// mode list and gets called back when the card is tapped. Choosing the ruleset and starting the match
// happen in the setup popup that the tap opens.
//
// Every part except the name is optional: the Play Online card keeps its own fixed title.
public class ModeCard : MonoBehaviour
{
    [SerializeField] private GameModeType mode;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject onlineBadge;

    private ModeInfo info;
    private Action<ModeInfo> onClick;

    public GameModeType Mode => mode;

    public void Setup(ModeInfo info, Action<ModeInfo> onClick)
    {
        this.info = info;
        this.onClick = onClick;

        if (iconImage != null)
        {
            iconImage.sprite = info.modeIcon;
        }

        if (nameText != null)
        {
            nameText.text = info.modeName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = info.modeDescription;
        }

        if (onlineBadge != null)
        {
            onlineBadge.SetActive(info.showOnlineBadge);
        }
    }

    public void OnCardButtonClick()
    {
        onClick?.Invoke(info);
    }
}
