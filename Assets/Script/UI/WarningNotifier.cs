using DG.Tweening;
using TMPro;
using UnityEngine;

public class WarningNotifier : Service<WarningNotifier>
{
    [SerializeField] private RectTransform contentTransform;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private float PopInDuration = 0.3f;
    [SerializeField] private float HoldDuration = 2f;
    [SerializeField] private float PopOutDuration = 0.3f;

    private float holdTimer;
    private bool isHolding;
    private bool isShowing;

    protected override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isShowing || !isHolding)
            return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= HoldDuration)
        {
            isHolding = false;

            // Pop out
            contentTransform
                .DOScale(Vector3.zero, PopOutDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    isShowing = false;
                    gameObject.SetActive(false);
                });
        }
    }

    public void Show(string description)
    {
        // Make sure the GameObject is visible
        gameObject.SetActive(true);

        // Set message
        descriptionText.text = description;

        // Stop any existing animation
        contentTransform.DOKill();

        // Always start from zero
        contentTransform.localScale = Vector3.zero;

        // Reset state
        holdTimer = 0f;
        isHolding = false;
        isShowing = true;

        // Pop in
        contentTransform
            .DOScale(Vector3.one, PopInDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // Start hold timer after pop-in completes
                holdTimer = 0f;
                isHolding = true;
            });
    }

    protected override void OnDestroy()
    {
        contentTransform.DOKill();
        base.OnDestroy();
    }
}
