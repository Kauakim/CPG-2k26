using UnityEngine;
using UnityEngine.Rendering.Universal;

/// Ponte entre core.cs (lógica/UI) e os objetos visuais da SampleScene.
/// Adicione este componente a um GameObject vazio na cena e configure
/// as referências via Inspector antes de entrar em Play.
public class SceneBridge : MonoBehaviour
{
    [Header("Personagens 1-8 (filhos de CENTRO)")]
    [SerializeField] private Transform[] characterSlots = new Transform[8];

    [Header("Luz de destaque (filho de LUZES → Atual Personagem)")]
    [SerializeField] private Transform atualPersonagemLight;

    private Light2D spotLight;

    private void Awake()
    {
        if (atualPersonagemLight != null)
            spotLight = atualPersonagemLight.GetComponent<Light2D>();
    }

    /// Move a luz de destaque para o personagem do índice informado e a ativa.
    /// Passe -1 para desligar a luz.
    public void SetActivePlayer(int playerIndex)
    {
        if (atualPersonagemLight == null) return;

        if (playerIndex < 0 || playerIndex >= characterSlots.Length || characterSlots[playerIndex] == null)
        {
            if (spotLight != null) spotLight.enabled = false;
            return;
        }

        Vector3 charPos = characterSlots[playerIndex].position;
        atualPersonagemLight.position = new Vector3(charPos.x, charPos.y + 3, atualPersonagemLight.position.z);
        if (spotLight != null) spotLight.enabled = true;
    }

    /// Ativa os primeiros <count> personagens e desativa o restante.
    public void SetPlayerCount(int count)
    {
        for (int i = 0; i < characterSlots.Length; i++)
        {
            if (characterSlots[i] != null)
                characterSlots[i].gameObject.SetActive(i < count);
        }
    }

    /// Desliga a luz e oculta todos os personagens (estado inicial / lobby zerado).
    public void ResetAll()
    {
        if (spotLight != null) spotLight.enabled = false;
        SetPlayerCount(0);
    }

    /// Retorna o Transform do personagem no índice indicado (pode ser null).
    public Transform GetCharacterTransform(int index)
    {
        if (index < 0 || index >= characterSlots.Length) return null;
        return characterSlots[index];
    }

    /// Retorna o sprite do SpriteRenderer do personagem indicado (pode ser null).
    public Sprite GetPlayerSprite(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= characterSlots.Length) return null;
        if (characterSlots[playerIndex] == null) return null;
        SpriteRenderer sr = characterSlots[playerIndex].GetComponent<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}
