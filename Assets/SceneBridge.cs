using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Ponte entre HPGameManager (lógica/UI) e os objetos visuais da cena.
public class SceneBridge : MonoBehaviour
{
    [Header("Personagens 1-8")]
    [SerializeField] private Transform[] characterSlots = new Transform[8];

    [Header("Luz de destaque do jogador atual")]
    [SerializeField] private Transform atualPersonagemLight;

    [Header("Seta giratória (pivot centralizado na mesa)")]
    [SerializeField] private Transform arrowPivot;
    [SerializeField] private float     arrowRotateSpeed = 720f; // graus/s de suavização

    [Header("DERRAUNDE (objeto que segue o jogador atual)")]
    [SerializeField] private Transform derraUndeObject;
    [SerializeField] private Vector3   derraUndeOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private float     derraUndeMoveSpeed = 8f; // lerp de posição

    // ── Estado interno ────────────────────────────────────────────────────────

    private Light2D spotLight;
    private int     activePlayerIndex = -1;
    private float   arrowTargetAngle;
    private bool    hasTarget;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (atualPersonagemLight != null)
            spotLight = atualPersonagemLight.GetComponent<Light2D>();
    }

    private void Update()
    {
        UpdateArrow();
        UpdateDerraunde();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// Define o jogador ativo. Passe -1 para desligar luz e seta.
    public void SetActivePlayer(int playerIndex)
    {
        activePlayerIndex = playerIndex;

        // Luz de destaque
        if (atualPersonagemLight == null) return;
        if (playerIndex < 0 || playerIndex >= characterSlots.Length || characterSlots[playerIndex] == null)
        {
            if (spotLight != null) spotLight.enabled = false;
            return;
        }

        Vector3 charPos = characterSlots[playerIndex].position;
        atualPersonagemLight.position = new Vector3(charPos.x,
                                                    charPos.y + 3f,
                                                    atualPersonagemLight.position.z);
        if (spotLight != null) spotLight.enabled = true;
    }

    /// Sinaliza para a seta apontar para o jogador. Chamado junto com SetActivePlayer.
    public void SetArrowTarget(int playerIndex)
    {
        activePlayerIndex = playerIndex;
        RecalcArrowAngle();
    }

    /// Sinaliza para o DERRAUNDE se mover para o jogador. Chamado junto com SetActivePlayer.
    public void SetDerraunde(int playerIndex)
    {
        activePlayerIndex = playerIndex;
        // A movimentação acontece em Update via lerp — nada a fazer aqui além de atualizar o índice.
        if (derraUndeObject != null)
            derraUndeObject.gameObject.SetActive(playerIndex >= 0);
    }

    /// Ativa os primeiros <count> personagens e desativa o restante.
    public void SetPlayerCount(int count)
    {
        for (int i = 0; i < characterSlots.Length; i++)
            if (characterSlots[i] != null)
                characterSlots[i].gameObject.SetActive(i < count);
    }

    /// Desliga luz, seta e DERRAUNDE; oculta todos os personagens.
    public void ResetAll()
    {
        activePlayerIndex = -1;
        hasTarget         = false;
        if (spotLight      != null) spotLight.enabled = false;
        if (derraUndeObject != null) derraUndeObject.gameObject.SetActive(false);
        SetPlayerCount(0);
    }

    /// Retorna o Transform do personagem no índice indicado.
    public Transform GetCharacterTransform(int index)
    {
        if (index < 0 || index >= characterSlots.Length) return null;
        return characterSlots[index];
    }

    /// Tinta o sprite do personagem de vermelho ao ser eliminado.
    /// Chame com eliminated=false para restaurar a cor original.
    public void SetPlayerEliminated(int playerIndex, bool eliminated)
    {
        Transform t = GetCharacterTransform(playerIndex);
        if (t == null) return;
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        sr.color = eliminated ? new Color(1f, 0.28f, 0.28f, 1f) : Color.white;
    }

    /// Retorna o sprite do SpriteRenderer do personagem indicado.
    public Sprite GetPlayerSprite(int playerIndex)
    {
        Transform t = GetCharacterTransform(playerIndex);
        if (t == null) return null;
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }

    // ── Atualização contínua da seta ──────────────────────────────────────────

    private void RecalcArrowAngle()
    {
        if (arrowPivot == null) { hasTarget = false; return; }
        Transform target = GetCharacterTransform(activePlayerIndex);
        if (target == null) { hasTarget = false; return; }

        Vector3 dir = target.position - arrowPivot.position;
        if (dir.sqrMagnitude <= 0.0001f) { hasTarget = false; return; }

        // Atan2 com -90° para que "cima" do sprite aponte na direção calculada
        arrowTargetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        hasTarget = true;
    }

    private void UpdateArrow()
    {
        if (arrowPivot == null) return;

        // Recalcula o alvo a cada frame (personagens podem se mover)
        RecalcArrowAngle();

        if (!hasTarget) return;

        float current = arrowPivot.eulerAngles.z;
        float next    = Mathf.MoveTowardsAngle(current, arrowTargetAngle,
                                               arrowRotateSpeed * Time.deltaTime);
        arrowPivot.rotation = Quaternion.Euler(0f, 0f, next);
    }

    // ── Atualização contínua do DERRAUNDE ─────────────────────────────────────

    private void UpdateDerraunde()
    {
        if (derraUndeObject == null) return;
        Transform target = GetCharacterTransform(activePlayerIndex);
        if (target == null) return;

        // Offset em espaço de mundo — não depende da rotação/escala do personagem
        Vector3 targetPos = new Vector3(
            target.position.x + derraUndeOffset.x,
            target.position.y + derraUndeOffset.y,
            derraUndeObject.position.z);   // preserva Z original para renderização

        derraUndeObject.position = Vector3.Lerp(
            derraUndeObject.position, targetPos,
            derraUndeMoveSpeed * Time.deltaTime);
    }
}