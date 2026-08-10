using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class opChoice : MonoBehaviour
{
    private RectTransform root;
    private Font font;
    private GameObject panel;
    private Action<string> callback;
    private Coroutine timeoutRoutine;

    public void Initialize(RectTransform canvasRoot, Font uiFont)
    {
        root = canvasRoot;
        font = uiFont;
    }

    public void Show(string[] options, float timeout, Action<string> onChosen)
    {
        Hide();
        callback = onChosen;

        panel = scene.CreateUIObject("OpChoicePanel", root);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -20f);
        panelRect.sizeDelta = new Vector2(760f, 240f);

        Image bg = panel.AddComponent<Image>();
        bg.color = Color.clear;

        GameObject titleObj = scene.CreateUIObject("Title", panel.transform);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -10f);
        titleRect.sizeDelta = new Vector2(0f, 38f);
        Text title = scene.CreateText(titleObj, "Como complicar?", 22, font, TextAnchor.MiddleCenter, scene.Hex("#ffdf4d"));
        title.fontStyle = FontStyle.Bold;

        float[] xPositions = new float[] { -240f, 0f, 240f };
        for (int i = 0; i < options.Length && i < 3; i++)
        {
            string captured = options[i];
            GameObject btn = scene.CreateButton("Op" + i, panel.transform, captured, font, delegate { Choose(captured); });
            RectTransform btnRect = btn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(xPositions[i], -30f);
            btnRect.sizeDelta = new Vector2(210f, 120f);

            Image btnImg = btn.GetComponent<Image>();
            if (btnImg != null)
            {
                btnImg.sprite = scene.CreateRoundedRectSprite(210, 120, 14, scene.Hex("#ffffff"), scene.Hex("#ffffff"), 0);
                btnImg.type = Image.Type.Sliced;
                btnImg.color = new Color(1f, 1f, 1f, 0.08f);
            }

            Text btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.fontSize = 32;
                btnText.fontStyle = FontStyle.Bold;
                btnText.alignment = TextAnchor.MiddleCenter;
            }
        }

        timeoutRoutine = StartCoroutine(TimeoutRoutine(options[UnityEngine.Random.Range(0, options.Length)], timeout));
    }

    public void Hide()
    {
        if (timeoutRoutine != null)
        {
            StopCoroutine(timeoutRoutine);
            timeoutRoutine = null;
        }
        callback = null;
        if (panel != null)
        {
            Destroy(panel);
            panel = null;
        }
    }

    private void Choose(string op)
    {
        Action<string> cb = callback;
        Hide();
        if (cb != null) cb(op);
    }

    private IEnumerator TimeoutRoutine(string autoChoice, float timeout)
    {
        yield return new WaitForSeconds(timeout);
        Choose(autoChoice);
    }
}
