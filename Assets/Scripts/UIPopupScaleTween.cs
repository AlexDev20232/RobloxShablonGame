using UnityEngine;

public class UIPopupScaleTween : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform target;

    [Header("Behavior")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool deactivateOnClose = true;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Open")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private float overshootScale = 1.05f;

    [Header("Close")]
    [SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private float closeScale = 0f;

    private Vector3 baseScale = Vector3.one;
    private int openTweenId = -1;
    private int returnTweenId = -1;
    private int closeTweenId = -1;
    private bool isClosing;

    private void Awake()
    {
        if (target == null)
            target = transform as RectTransform;

        CacheBaseScale();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayOpen();
    }

    private void OnDisable()
    {
        CancelTween(ref openTweenId);
        CancelTween(ref returnTweenId);
        CancelTween(ref closeTweenId);
        isClosing = false;
    }

    public void PlayOpen()
    {
        if (target == null)
            return;

        isClosing = false;
        CacheBaseScale();
        CancelTween(ref closeTweenId);
        CancelTween(ref openTweenId);
        CancelTween(ref returnTweenId);

        target.localScale = Vector3.one * closeScale;

        float overshoot = Mathf.Max(1f, overshootScale);
        LTDescr openTween = LeanTween.scale(target.gameObject, baseScale * overshoot, openDuration)
            .setEase(LeanTweenType.easeOutBack);
        if (useUnscaledTime)
            openTween.setIgnoreTimeScale(true);
        openTweenId = openTween.uniqueId;

        LTDescr returnTween = LeanTween.scale(target.gameObject, baseScale, openDuration * 0.6f)
            .setEase(LeanTweenType.easeOutSine);
        if (useUnscaledTime)
            returnTween.setIgnoreTimeScale(true);
        returnTweenId = returnTween.uniqueId;
    }

    public void PlayClose()
    {
        if (isClosing || target == null)
            return;

        isClosing = true;
        CacheBaseScale();
        CancelTween(ref openTweenId);
        CancelTween(ref returnTweenId);
        CancelTween(ref closeTweenId);

        LTDescr closeTween = LeanTween.scale(target.gameObject, Vector3.one * closeScale, closeDuration)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                isClosing = false;
                if (deactivateOnClose)
                    gameObject.SetActive(false);
            });
        if (useUnscaledTime)
            closeTween.setIgnoreTimeScale(true);
        closeTweenId = closeTween.uniqueId;
    }

    private void CacheBaseScale()
    {
        if (target == null)
            return;

        if (target.localScale.sqrMagnitude > 0.0001f)
            baseScale = target.localScale;
    }

    private static void CancelTween(ref int tweenId)
    {
        if (tweenId < 0)
            return;

        LeanTween.cancel(tweenId);
        tweenId = -1;
    }
}
