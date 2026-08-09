using UnityEngine;
using UnityEngine.UI;

public class scene : MonoBehaviour
{
    private const float UI_TEXT_SCALE = 1.3f;
    public static Font BuiltInFont()
    {
        Font font = TryBuiltInFont("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        font = TryBuiltInFont("Arial.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", 16);
    }

    private static Font TryBuiltInFont(string fontName)
    {
        try
        {
            return Resources.GetBuiltinResource<Font>(fontName);
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }

    public static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition3D = Vector3.zero;
        return obj;
    }

    public static Text CreateText(GameObject obj, string value, int size, Font font, TextAnchor alignment, Color color)
    {
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = Mathf.CeilToInt(size * UI_TEXT_SCALE);
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        obj.transform.SetAsLastSibling();

        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
        shadow.effectDistance = new Vector2(1f, -1f);

        return text;
    }

    public static GameObject CreateButton(string name, Transform parent, string label, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = CreateUIObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.sprite = CreateRoundedRectSprite(220, 64, 14, Color.white, Color.white, 0);
        image.type = Image.Type.Sliced;
        image.color = Hex("#17304d");

        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.normalColor = Hex("#17304d");
        colors.highlightedColor = Hex("#25557c");
        colors.pressedColor = Hex("#0e2239");
        colors.selectedColor = Hex("#25557c");
        colors.disabledColor = new Color(0.12f, 0.12f, 0.16f, 0.55f);
        button.colors = colors;

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.88f, 1f, 0.42f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject textObject = CreateUIObject("Text", obj.transform);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect);
        Text text = CreateText(textObject, label, 20, font, TextAnchor.MiddleCenter, Color.white);
        text.fontStyle = FontStyle.Bold;

        return obj;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void AnchorTop(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    public static Color Hex(string value)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(value, out color))
        {
            return color;
        }

        return Color.white;
    }

    public static Sprite CreateRoundedRectSprite(int width, int height, int radius, Color fill, Color outline, int outlineWidth)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 half = new Vector2(width * 0.5f, height * 0.5f);
        Vector2 roundedHalf = new Vector2(Mathf.Max(0f, half.x - radius), Mathf.Max(0f, half.y - radius));

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 point = new Vector2(Mathf.Abs(x + 0.5f - half.x), Mathf.Abs(y + 0.5f - half.y));
                Vector2 q = point - roundedHalf;
                float outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
                float inside = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
                float distance = outside + inside - radius;
                Color pixel = Color.clear;

                if (distance <= 0f)
                {
                    pixel = outlineWidth > 0 && distance >= -outlineWidth ? outline : fill;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    public static Sprite CreateTriangleSprite(int size, Color fill)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float ny = y / (float)(size - 1);
                float nx = x / (float)(size - 1);
                float halfWidth = (1f - ny) * 0.47f;
                bool inside = ny <= 0.96f && Mathf.Abs(nx - 0.5f) <= halfWidth;
                texture.SetPixel(x, y, inside ? fill : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public static Sprite CreateCircleSprite(int size, Color fill, Color outline, int outlineWidth)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float radius = size * 0.5f - 1f;
        float outlineStart = Mathf.Max(0f, radius - outlineWidth);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                Color pixel = Color.clear;

                if (distance <= radius)
                {
                    pixel = distance >= outlineStart && outlineWidth > 0 ? outline : fill;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
