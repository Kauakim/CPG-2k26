using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Controla a vinheta do Global Volume com base no tempo restante do turno.
/// Adicione este componente a qualquer GameObject na cena e atribua o Volume via Inspector.
public class HPVignetteController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Volume       globalVolume;
    [SerializeField] private HPTimerSystem timerSystem;

    [Header("Cor da vinheta no início da partida")]
    [SerializeField] private Color startColor = new Color(0f, 0f, 0f);       // preto neutro

    [Header("Cor da vinheta no pico (tempo crítico)")]
    [SerializeField] private Color peakColor  = new Color(0.72f, 0.05f, 0.05f); // vermelho escuro

    [Header("Intensidade")]
    [SerializeField] private float startIntensity = 0.15f;  // intensidade ao começar
    [SerializeField] private float maxIntensity   = 0.55f;  // no último segundo
    [SerializeField] private float smoothSpeed    = 4f;

    [Header("Fração do timer que começa a crescer (0–1)")]
    [Tooltip("0.4 = a vinheta começa a mudar quando restar 40% do tempo")]
    [SerializeField] private float startFraction = 0.4f;

    private Vignette vignette;
    private float    currentIntensity;
    private Color    currentColor;
    private bool     isGameActive;

    private void Awake()
    {
        if (globalVolume != null)
            globalVolume.profile.TryGet(out vignette);
    }

    private void Update()
    {
        if (vignette == null || timerSystem == null || !isGameActive) return;

        float t = CalculateProgress();   // 0 = tempo cheio, 1 = tempo crítico

        float targetIntensity = Mathf.Lerp(startIntensity, maxIntensity, t * t);
        Color targetColor     = Color.Lerp(startColor, peakColor, t);

        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, smoothSpeed * Time.deltaTime);
        currentColor     = Color.Lerp(currentColor,     targetColor,     smoothSpeed * Time.deltaTime);

        vignette.intensity.Override(currentIntensity);
        vignette.color.Override(currentColor);
    }

    /// Chame em BeginGame — inicia com a cor e intensidade iniciais.
    public void StartGame()
    {
        isGameActive     = true;
        currentIntensity = startIntensity;
        currentColor     = startColor;
    }

    /// Zera a vinheta e desativa o controle (EndGame / ResetToLobby).
    public void ResetVignette()
    {
        isGameActive     = false;
        currentIntensity = 0f;
        currentColor     = startColor;
        if (vignette == null) return;
        vignette.intensity.Override(0f);
    }

    // ── Privado ───────────────────────────────────────────────────────────────

    /// Retorna um valor 0–1: 0 = timer cheio, 1 = tempo crítico/zerado.
    private float CalculateProgress()
    {
        if (!timerSystem.Running || timerSystem.Remaining <= 0f)
            return 0f;

        float fraction = timerSystem.Remaining /
                         Mathf.Max(0.01f, timerSystem.Remaining + timerSystem.Elapsed);

        if (fraction >= startFraction) return 0f;

        return 1f - (fraction / startFraction);   // linear 0→1 conforme o tempo acaba
    }
}