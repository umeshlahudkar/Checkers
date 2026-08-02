using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour
{
    [SerializeField] private Image trackImage;
    [SerializeField] private Image knobImage;
    [SerializeField] private RectTransform knobTransform;
    [SerializeField] private Sprite onTrackSprite;
    [SerializeField] private Sprite offTrackSprite;
    [SerializeField] private Sprite onKnobSprite;
    [SerializeField] private Sprite offKnobSprite;
    [SerializeField] private float knobOffsetX = 24f;

    private bool isOn;
    private Action<bool> onValueChanged;

    public void Setup(bool value, Action<bool> onChanged)
    {
        onValueChanged = onChanged;
        Apply(value);
    }

    public void OnToggleClick()
    {
        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        Apply(!isOn);
        onValueChanged?.Invoke(isOn);
    }

    private void Apply(bool value)
    {
        isOn = value;
        trackImage.sprite = isOn ? onTrackSprite : offTrackSprite;
        knobImage.sprite = isOn ? onKnobSprite : offKnobSprite;

        Vector2 pos = knobTransform.anchoredPosition;
        pos.x = isOn ? knobOffsetX : -knobOffsetX;
        knobTransform.anchoredPosition = pos;
    }
}
