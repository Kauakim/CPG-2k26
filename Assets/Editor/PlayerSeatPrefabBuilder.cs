using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class PlayerSeatPrefabBuilder
{
    private const string PLAYERUI_PATH = "Assets/Prefabs/PlayerUI.prefab";

    /// Cria um prefab de UI compacto para ser filho do GameObject Personagem na cena.
    /// Contém apenas as informações do jogador — o sprite do personagem já é o avatar
    /// e a Light2D "Atual Personagem" já indica quem é o jogador ativo.
    [MenuItem("HotPotato/Criar Prefab PlayerUI")]
    public static void CreatePlayerUIPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        Font font = GetBuiltInFont();

        // ── Canvas World Space ─────────────────────────────────────────────────
        // Será filho do Personagem (world space), então usa World Space canvas.
        // Scale 0.006 → 300 px canvas = 1.8 unidades de mundo, adequado para
        // personagens de escala 2.5 com câmera ortográfica size 5.
        GameObject root = new GameObject("PlayerUI");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(300f, 160f);
        rootRect.localScale = new Vector3(0.006f, 0.006f, 0.006f);
        // Posiciona acima do centro do sprite — ajuste conforme o personagem
        rootRect.localPosition = new Vector3(0f, 1.8f, 0f);

        // Aumenta a densidade de pixels do canvas no espaço do mundo,
        // eliminando o embaçamento do texto em World Space canvas.
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        CanvasGroup group = root.AddComponent<CanvasGroup>();

        // ── Name ──────────────────────────────────────────────────────────────
        // Topo do painel, nome do jogador
        GameObject nameGO = Child(root, "Name", 280f, 30f, 0f, 56f);
        Text nameText = nameGO.AddComponent<Text>();
        nameText.font = font;
        nameText.fontSize = 24;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = Color.white;
        nameText.text = "Slot";
        nameGO.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        // ── LivesImage ────────────────────────────────────────────────────────
        // Sprite de barra de vidas (atribuir no Inspector os sprites de Resources/LifeBar)
        GameObject livesImgGO = Child(root, "LivesImage", 180f, 28f, 0f, 18f);
        Image livesImg = livesImgGO.AddComponent<Image>();
        livesImg.preserveAspect = true;

        // ── Lives (texto fallback) ────────────────────────────────────────────
        // Usado se os sprites de vida não estiverem atribuídos
        GameObject livesTextGO = Child(root, "Lives", 180f, 22f, 0f, 18f);
        Text livesText = livesTextGO.AddComponent<Text>();
        livesText.font = font;
        livesText.fontSize = 14;
        livesText.alignment = TextAnchor.MiddleCenter;
        livesText.color = Hex("#FF5D7A");
        livesText.text = "♥♥♥";
        livesTextGO.SetActive(false);

        // ── Cards container ───────────────────────────────────────────────────
        // Texto do nome da carta (fallback)
        GameObject cardsGO = Child(root, "Cards", 120f, 22f, -60f, -18f);
        Text cardText = cardsGO.AddComponent<Text>();
        cardText.font = font;
        cardText.fontSize = 13;
        cardText.fontStyle = FontStyle.Bold;
        cardText.alignment = TextAnchor.MiddleCenter;
        cardText.color = Hex("#FFDF4D");

        // CardImage — ícone da carta, canto direito inferior
        GameObject cardImgGO = Child(root, "CardImage", 50f, 50f, 110f, -20f);
        Image cardImg = cardImgGO.AddComponent<Image>();
        cardImg.preserveAspect = true;
        cardImgGO.SetActive(false);

        // ── Badge ─────────────────────────────────────────────────────────────
        // Exibido quando o jogador é eliminado ("DP") ou recebe NP3 ("NP3")
        GameObject badgeGO = Child(root, "Badge", 150f, 38f, 0f, -56f);
        Text badgeText = badgeGO.AddComponent<Text>();
        badgeText.font = font;
        badgeText.text = "NP3";
        badgeText.fontSize = 26;
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.alignment = TextAnchor.MiddleCenter;
        badgeText.color = Hex("#FF1744");
        badgeGO.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        badgeGO.SetActive(false);

        // ── Salvar prefab ─────────────────────────────────────────────────────
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PLAYERUI_PATH);
        Object.DestroyImmediate(root);

        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log("[HotPotato] PlayerUI prefab criado em: " + PLAYERUI_PATH);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject Child(GameObject parent, string name, float w, float h, float px, float py)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(px, py);
        return go;
    }

    private static Color Hex(string hex)
    {
        Color c;
        ColorUtility.TryParseHtmlString(hex, out c);
        return c;
    }

    private static Font GetBuiltInFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return f;
    }
}
