using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class PageManager<TSelf, TEnum> : Singleton<TSelf>
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
    private Page currentPage;
    private TEnum currentKey;

    private void Awake()
    {
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

    public void GoBack()
    {
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

    public bool IsPageOpen(TEnum key)
    {
        return currentPage != null && pages.TryGetValue(key, out Page page) && page == currentPage;
    }

    private void SwitchTo(TEnum key)
    {
        currentPage?.Close();
        currentKey = key;
        currentPage = pages[key];
        currentPage.Open();
        OnPageOpened(key);
    }

    protected virtual void OnPageOpened(TEnum key) { }
    protected virtual void OnPageClosed() { }
}
