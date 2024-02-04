using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour
{
    [SerializeField] private Transform rotatingObjTran;
    [SerializeField] private Image RotatingImage;
    [SerializeField] private AnimationCurve animationCurve;

    [SerializeField] private float rotationSpeed;

    private readonly float imgFillTime = 1.5f;
    private float currentTime = 0;

    private void OnEnable()
    {
        currentTime = 0;
        rotatingObjTran.rotation = Quaternion.Euler(0, 0, 0);
    }

    private void Update()
    {
        rotatingObjTran.Rotate(0, 0, -Time.deltaTime * rotationSpeed);

        currentTime += Time.deltaTime;
        RotatingImage.fillAmount = animationCurve.Evaluate( currentTime/imgFillTime );
        if (currentTime >= imgFillTime)
        {
            currentTime = 0.1f;
        }
    }
}
