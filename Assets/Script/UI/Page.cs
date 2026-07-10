using UnityEngine;

public class Page : MonoBehaviour
{
    public bool IsOpen { get; private set; }

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

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }
}
