using UnityEngine;

public class Page : MonoBehaviour
{
    public bool IsOpen { get; protected set; }

    public virtual void Open()
    {
        IsOpen = true;
        gameObject.SetActive(true);
        OnOpened();
    }

    public virtual void Close()
    {
        IsOpen = false;
        gameObject.SetActive(false);
        OnClosed();
    }

    // The device back button was pressed while this page was open - see BackButtonHandler. Return
    // true if the page dealt with it (including deliberately doing nothing), false to let the press
    // fall through to whatever is underneath. Pages with no back behaviour of their own, like the
    // shared top and bottom bars, leave this alone.
    public virtual bool OnBackPressed()
    {
        return false;
    }

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
}
