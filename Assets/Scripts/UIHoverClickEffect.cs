using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class UIHoverClickEffect : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.08f;
    public float hoverDuration = 0.12f;
    public bool useUnscaledTime = true;

    [Header("Click / Pulse Settings")]
    public float clickScale = 1.15f;
    public float clickDuration = 0.18f;
    public bool clickAlwaysPlays = false;

    [Header("Idle Pulse Settings")]
    public bool enableIdlePulse = true;
    public float idleScale = 1.05f;
    public float idleSpeed = 1.5f;

    [Header("Answer Feedback Settings")]
    public bool enableAnswerFeedback = false;
    public bool isCorrect = false;
    public bool isWrong = false;
    public bool keepCorrectColor = true;  

    public float blinkDuration = 0.5f;
    public int blinkCount = 2;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [Header("Events (Optional)")]
    public UnityEvent onHoverEnter;
    public UnityEvent onHoverExit;
    public UnityEvent onClick;

    RectTransform rt;
    Vector3 baseScale;
    Coroutine scaleCoroutine;
    Coroutine blinkCoroutine;
    bool isHovered = false;
    Selectable selectableComponent;
    Graphic graphicComponent;
    Color originalColor;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        baseScale = rt.localScale;
        selectableComponent = GetComponent<Selectable>();
        graphicComponent = GetComponent<Graphic>();

        if (graphicComponent != null)
            originalColor = graphicComponent.color;
    }

    bool IsInteractable()
    {
        return selectableComponent == null || selectableComponent.interactable;
    }

    void Update()
    {
        if (!enableIdlePulse) return;
        if (!IsInteractable()) return;
        if (isHovered) return;
        if (scaleCoroutine != null) return;

        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float pulse = 1f + Mathf.Sin(time * idleSpeed) * (idleScale - 1f);

        rt.localScale = baseScale * pulse;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovered = true;
        StopScaleCoroutine();

        Vector3 target = baseScale * hoverScale;
        scaleCoroutine = StartCoroutine(ScaleTo(rt.localScale, target, hoverDuration));

        onHoverEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        isHovered = false;
        StopScaleCoroutine();

        scaleCoroutine = StartCoroutine(ScaleTo(rt.localScale, baseScale, hoverDuration));

        onHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        AudioManager.instance?.PlayClick();
        onClick?.Invoke();

        // If the click callback deactivated this object or its parent, do not start coroutines
        if (!gameObject.activeInHierarchy) return;

        if (!clickAlwaysPlays && !isHovered) return;

        StopScaleCoroutine();
        if (gameObject.activeInHierarchy)
        {
            scaleCoroutine = StartCoroutine(ClickPulseCoroutine());
        }

        // Answer Feedback Logic
        if (enableAnswerFeedback && gameObject.activeInHierarchy)
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(BlinkFeedback());
        }
    }

    IEnumerator BlinkFeedback()
    {
        if (graphicComponent == null)
            yield break;

        Color targetColor = originalColor;

        if (isCorrect)
            targetColor = correctColor;
        else if (isWrong)
            targetColor = wrongColor;

        float half = blinkDuration / (blinkCount * 2f);

        for (int i = 0; i < blinkCount; i++)
        {
            graphicComponent.color = targetColor;
            yield return new WaitForSecondsRealtime(half);

            graphicComponent.color = originalColor;
            yield return new WaitForSecondsRealtime(half);
        }

        // FINAL COLOR AFTER BLINK
        if (isCorrect && keepCorrectColor)
        {
            graphicComponent.color = correctColor;
        }
        else
        {
            graphicComponent.color = originalColor;
        }
    }

    void StopScaleCoroutine()
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
    }

    IEnumerator ScaleTo(Vector3 start, Vector3 target, float duration)
    {
        if (duration <= 0f)
        {
            rt.localScale = target;
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, f);

            rt.localScale = Vector3.LerpUnclamped(start, target, eased);
            yield return null;
        }

        rt.localScale = target;
        scaleCoroutine = null;
    }

    IEnumerator ClickPulseCoroutine()
    {
        Vector3 start = rt.localScale;
        Vector3 peak = start * clickScale;
        float half = clickDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / half);
            float eased = Mathf.SmoothStep(0f, 1f, f);
            rt.localScale = Vector3.LerpUnclamped(start, peak, eased);
            yield return null;
        }

        Vector3 returnTarget = isHovered ? baseScale * hoverScale : baseScale;
        t = 0f;

        while (t < half)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
            float f = Mathf.Clamp01(t / half);
            float eased = Mathf.SmoothStep(0f, 1f, f);
            rt.localScale = Vector3.LerpUnclamped(peak, returnTarget, eased);
            yield return null;
        }

        rt.localScale = returnTarget;
        scaleCoroutine = null;
    }

    public void ResetBaseScaleToCurrent()
    {
        baseScale = rt.localScale;
    }

    private void OnDisable()
    {
        StopScaleCoroutine();
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        isHovered = false;
        if (rt != null && baseScale != Vector3.zero)
        {
            rt.localScale = baseScale;
        }
    }
}