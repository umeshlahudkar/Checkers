using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class SessionManager : Singleton<SessionManager>
{
    private TimeSpan backgroundThreshHoldTimeSpan = new TimeSpan(0, 0, 120);
    private DateTime pausedDateTime;
    private bool isFreshStart = true;

    private void Start()
    {
        CheckForSessionStatus();
    }

    private void OnApplicationFocus(bool focus)
    {
        if(focus)
        {
            if(!isFreshStart)
            {
                Debug.Log("focused");
                TimeSpan pausedSpan = DateTime.Now - pausedDateTime;

                if (pausedSpan.TotalSeconds > backgroundThreshHoldTimeSpan.TotalSeconds)
                {
                    CheckForSessionStatus();
                }
            }
        }
        else
        {
            Debug.Log("unfocused");
            isFreshStart = false;
            pausedDateTime = DateTime.Now;

            SaveData data = SavingSystem.Instance.Load();
            data.sessionInfo.lastOpenDate = DateTime.Now.ToBinary();
            SavingSystem.Instance.Save(data);
        }
    }

    private void CheckForSessionStatus()
    {
        SaveData data = SavingSystem.Instance.Load();
        SessionInfo sessionInfo = data.sessionInfo;

        if (sessionInfo != null)
        {
            Debug.Log("First open : " + DateTime.FromBinary(sessionInfo.firstOpenDate));
            Debug.Log("last open : " + DateTime.FromBinary(sessionInfo.lastOpenDate));

            TimeSpan span = DateTime.Now - DateTime.FromBinary(sessionInfo.lastOpenDate);

            int totalDays = Mathf.FloorToInt((float)span.TotalDays);
            int totalHrs = Mathf.FloorToInt((float)span.TotalHours);

            Debug.Log("Total days : " + totalDays);
            Debug.Log("Total hrs : " + totalHrs);

            if (totalDays <= 0)
            {
                sessionInfo.currentSessionOfDay++;
                Debug.Log("Session of Day " + sessionInfo.currentSessionOfDay);
            }
            else
            {
                sessionInfo.currentSessionOfDay = 0;
                sessionInfo.currentSessionCount++;
                Debug.Log("Current session count " + sessionInfo.currentSessionCount++);
            }

            SavingSystem.Instance.Save(data);
        }
    }

    private void OnApplicationQuit()
    {
        SaveData data = SavingSystem.Instance.Load();
        data.sessionInfo.lastOpenDate = DateTime.Now.ToBinary();
        SavingSystem.Instance.Save(data);
    }
}


[Serializable]
public class SessionInfo
{
    public long firstOpenDate;
    public long lastOpenDate;
    public int currentSessionCount;
    public int currentSessionOfDay;
}

