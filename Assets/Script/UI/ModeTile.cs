using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModeTile : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private ModeInfo modeInfo;
    private Action<ModeInfo> onButtonClick;

    public void Setup(ModeInfo info, Action<ModeInfo> onSelected)
    {
        modeInfo = info;
        onButtonClick = onSelected;

        iconImage.sprite = info.modeIcon;
        nameText.text = info.modeName;
        descriptionText.text = info.modeDescription;
    }

    public void OnButtonClick()
    {
        onButtonClick?.Invoke(modeInfo);
    }
}
