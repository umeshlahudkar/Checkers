using UnityEngine;
using TMPro;

public class MassageDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI msgText;
    [SerializeField] private GameObject faderScreen;

    private readonly float deactivateTime = 2f;
    private float currentTime = 0;

    public void ShowMassage(string msg, float deactivalteDelay = 0)
    {
        if(!gameObject.activeSelf)
        {
            currentTime = deactivalteDelay == 0 ? deactivateTime : deactivalteDelay ;
            gameObject.Activate();
            msgText.text = msg;
        }
    }

    private void Update()
    {
        if(currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if(currentTime <= 0)
            {
                currentTime = 0;
                faderScreen.SetActive(false);
                gameObject.Deactivate();
            }
        }
    }
}
