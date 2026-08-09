using UnityEngine;
using UnityEngine.UI;

/// Gerencia o temporizador de turno. Atualiza o Text do Inspector a cada Tick.
/// Não cria nenhum objeto — o Text deve ser atribuído via Inspector.
public class HPTimerSystem : MonoBehaviour
{
    [SerializeField] private Text timerLabel;
    [SerializeField] private bool showTimerLabel = false;

    public float Remaining { get; private set; }
    public float Elapsed   { get; private set; }
    public bool  Running   { get; private set; }
    public bool  ShouldBlink => Running && Remaining > 0f && Remaining <= 3f;

    private float maxTime = 30f;

    public void StartTimer(float seconds)
    {
        maxTime   = Mathf.Max(0.01f, seconds);
        Remaining = maxTime;
        Elapsed   = 0f;
        Running   = true;
        UpdateLabel();
    }

    public void Tick(float deltaTime)
    {
        if (!Running) return;
        Remaining = Mathf.Max(0f, Remaining - deltaTime);
        Elapsed   = maxTime - Remaining;
        UpdateLabel();
    }

    public void StopTimer()
    {
        Running = false;
        if (timerLabel != null) timerLabel.text = "";
    }

    public void Penalize(float seconds)
    {
        Remaining = Mathf.Max(0f, Remaining - seconds);
        Elapsed   = maxTime - Remaining;
        UpdateLabel();
    }

    public void SetRemainingTime(float seconds)
    {
        float prev = Elapsed;
        Remaining  = Mathf.Max(0.01f, seconds);
        maxTime    = Mathf.Max(maxTime, Remaining);
        Elapsed    = prev;
        Running    = true;
        UpdateLabel();
    }

    public int GetTime() => Mathf.CeilToInt(Remaining);

    public Color CurrentLightColor()
    {
        if (Remaining <= 0f)             return Color.clear;
        if (Remaining > maxTime - 5f)   return HPCardInfo.Hex("#26a7ff");
        if (Remaining > maxTime * 0.5f) return HPCardInfo.Hex("#27e36f");
        if (Remaining > maxTime * 0.25f)return HPCardInfo.Hex("#ffe04d");
        return HPCardInfo.Hex("#ff274f");
    }

    private void UpdateLabel()
    {
        if (timerLabel == null) return;
        timerLabel.text = showTimerLabel ? Mathf.CeilToInt(Remaining).ToString() : "";
    }
}