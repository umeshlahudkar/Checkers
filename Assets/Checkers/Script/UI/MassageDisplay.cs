using UnityEngine;
using TMPro;

public class MassageDisplay : Page
{
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject faderScreen;

    private readonly float deactivateTime = 2f;
    private float currentTime = 0;

    public void ShowMassage(string msg, float deactivalteDelay = 0)
    {
        if(!IsOpen)
        {
            currentTime = deactivalteDelay == 0 ? deactivateTime : deactivalteDelay;
            msgText.text = msg;
            Open();
        }
    }

    protected override void OnClosed()
    {
        faderScreen.SetActive(false);
        currentTime = 0;
        msgText.text = string.Empty;
    }

    private void Update()
    {
        if(currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if(currentTime <= 0)
            {
                Close();
            }
        }
    }
}
