using System;
using UnityEngine;

// Generic tap-to-scroll index picker: Left/Right buttons cycle through 0..itemCount-1, wrapping at
// the ends. Doesn't know anything about what's being displayed - callers refresh their own content
// (e.g. a single card's text) from the index-changed callback. Reusable anywhere a "pick one of N,
// one at a time, via left/right arrows" control is needed.
public class TappableCarousel : MonoBehaviour
{
    private int itemCount;
    private int currentIndex;
    private Action<int> onIndexChanged;

    public int CurrentIndex => currentIndex;

    public void Setup(int itemCount, int startIndex, Action<int> onIndexChanged)
    {
        this.itemCount = itemCount;
        this.onIndexChanged = onIndexChanged;
        currentIndex = itemCount > 0 ? Mathf.Clamp(startIndex, 0, itemCount - 1) : 0;
    }

    public void OnLeftButtonClick()
    {
        Move(-1);
    }

    public void OnRightButtonClick()
    {
        Move(1);
    }

    private void Move(int delta)
    {
        if (itemCount <= 1) { return; }

        ServiceLocator.Get<AudioManager>().PlayButtonClickSound();
        currentIndex = (currentIndex + delta + itemCount) % itemCount;
        onIndexChanged?.Invoke(currentIndex);
    }
}
