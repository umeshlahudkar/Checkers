using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PageManager<TSelf, TEnum> : Service<TSelf>
    where TSelf : Component
    where TEnum : Enum
{
    [Serializable]
    public class PageEntry
    {
        public TEnum key;
        public Page page;
    }

    [SerializeField] private List<PageEntry> pageEntries;

    private readonly Dictionary<TEnum, Page> pages = new();
    private readonly Stack<TEnum> pageStack = new();
    private readonly List<Page> overlayPages = new();
    private Page currentPage;
    private TEnum currentKey;

    protected override void Awake()
    {
        base.Awake();

        foreach (PageEntry entry in pageEntries)
        {
            pages[entry.key] = entry.page;
        }
    }

    public void OpenPage(TEnum key)
    {
        if (currentPage != null)
        {
            pageStack.Push(currentKey);
        }

        SwitchTo(key);
    }

    public void OpenPageAsOverlay(TEnum key)
    {
        Page page = pages[key];
        overlayPages.Add(page);
        page.Open();
        OnPageOpened(key);
    }

    public void CloseOverlay(TEnum key)
    {
        Page page = pages[key];
        if (overlayPages.Remove(page))
        {
            page.Close();
            OnPageClosed();
        }
    }

    public void GoBack()
    {
        if (overlayPages.Count > 0)
        {
            return;
        }

        if (pageStack.Count > 0)
        {
            SwitchTo(pageStack.Pop());
        }
        else
        {
            CloseCurrentPage();
        }
    }

    public void CloseCurrentPage()
    {
        CloseAllOverlays();
        currentPage?.Close();
        currentPage = null;
        pageStack.Clear();
        OnPageClosed();
    }

    public Page GetPage(TEnum key)
    {
        return pages.TryGetValue(key, out Page page) ? page : null;
    }

    public Page CurrentActivePage => currentPage;
    public TEnum CurrentPageKey => currentKey;

    public bool IsPageOpen(TEnum key)
    {
        return pages.TryGetValue(key, out Page page) && (page == currentPage || overlayPages.Contains(page));
    }

    private void SwitchTo(TEnum key)
    {
        CloseAllOverlays();
        currentPage?.Close();
        currentKey = key;
        currentPage = pages[key];
        currentPage.Open();
        OnPageOpened(key);
    }

    private void CloseAllOverlays()
    {
        for (int i = overlayPages.Count - 1; i >= 0; i--)
        {
            overlayPages[i].Close();
        }
        overlayPages.Clear();
    }

    protected virtual void OnPageOpened(TEnum key) { }
    protected virtual void OnPageClosed() { }
}
