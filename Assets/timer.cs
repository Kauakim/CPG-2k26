using UnityEngine;
using UnityEngine.UI;

public class timer : MonoBehaviour
{
    private Image fill;
    private Text label;
    private float maxTime = core.TIMER_MAX;

    public float Remaining { get; private set; }
    public float Elapsed { get; private set; }
    public bool Running { get; private set; }
    public bool ShouldBlink
    {
        get { return Running && Remaining > 0f && Remaining <= 3f; }
    }

    public void Initialize(Image fillImage, Text labelText)
    {
        fill = fillImage;
        label = labelText;
        ResetTimer();
    }

    public void StartTimer(float seconds)
    {
        maxTime = Mathf.Max(0.01f, seconds);
        Remaining = maxTime;
        Elapsed = 0f;
        Running = true;
    }

    public void Tick(float deltaTime)
    {
        if (!Running)
        {
            return;
        }

        Remaining = Mathf.Max(0f, Remaining - deltaTime);
        Elapsed = maxTime - Remaining;
    }

    public void Penalize(float seconds)
    {
        Remaining = Mathf.Max(0f, Remaining - seconds);
        Elapsed = maxTime - Remaining;
    }

    public void SetRemainingTime(float seconds)
    {
        float elapsedBeforeChange = Elapsed;
        Remaining = Mathf.Max(0.01f, seconds);
        maxTime = Mathf.Max(maxTime, Remaining);
        Elapsed = elapsedBeforeChange;
        Running = true;
    }

    public void StopTimer()
    {
        Running = false;
    }

    public void ResetTimer()
    {
        Remaining = maxTime;
        Elapsed = 0f;
        Running = false;
    }

    public Color CurrentLightColor()
    {
        if (Remaining <= 0f)
        {
            return Color.clear;
        }

        if (Remaining > core.TIMER_MAX - core.TIMER_BUFF)
        {
            return scene.Hex("#26a7ff");
        }

        if (Remaining > core.TIMER_MAX * 0.5f)
        {
            return scene.Hex("#27e36f");
        }

        if (Remaining > core.TIMER_MAX * 0.25f)
        {
            return scene.Hex("#ffe04d");
        }

        return scene.Hex("#ff274f");
    }

    public int GetTime()
    {
        return Mathf.CeilToInt(Remaining);
    }
}
