using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Painel "Como complicar?" com 3 botões de operação.
/// O painel deve estar pré-construído na cena (desativado por padrão).
public class HPOpChoiceView : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Text       titleText;
    [SerializeField] private Button     op1Button;
    [SerializeField] private Button     op2Button;
    [SerializeField] private Button     op3Button;
    [SerializeField] private Text       op1Label;
    [SerializeField] private Text       op2Label;
    [SerializeField] private Text       op3Label;

    private Action<string> callback;
    private Coroutine timeoutRoutine;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string[] options, float timeout, Action<string> onChosen)
    {
        Hide();
        callback = onChosen;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel == null) return;
        panel.SetActive(true);
        if (titleText != null) titleText.text = "Como complicar?";

        Button[] btns   = { op1Button, op2Button, op3Button };
        Text[]   labels = { op1Label,  op2Label,  op3Label  };

        for (int i = 0; i < 3; i++)
        {
            bool hasOp = i < options.Length;
            if (btns[i] != null) btns[i].gameObject.SetActive(hasOp);
            if (!hasOp) continue;

            string opt = options[i];
            if (btns[i] != null)
            {
                btns[i].onClick.RemoveAllListeners();
                btns[i].onClick.AddListener(() => Choose(opt));
            }
            if (labels[i] != null) labels[i].text = opt;
        }

        timeoutRoutine = StartCoroutine(TimeoutRoutine(
            options[UnityEngine.Random.Range(0, options.Length)], timeout));
    }

    public void Hide()
    {
        if (timeoutRoutine != null) { StopCoroutine(timeoutRoutine); timeoutRoutine = null; }
        callback = null;
        if (panel != null) panel.SetActive(false);
    }

    private void Choose(string op)
    {
        var cb = callback;
        Hide();
        cb?.Invoke(op);
    }

    private IEnumerator TimeoutRoutine(string autoChoice, float timeout)
    {
        yield return new WaitForSeconds(timeout);
        Choose(autoChoice);
    }
}
