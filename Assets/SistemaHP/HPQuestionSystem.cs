using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// Toda lógica de geração e manipulação de expressões matemáticas.
/// Nenhuma dependência de UI — comunicação somente via retorno de valores.
public class HPQuestionSystem : MonoBehaviour
{
    public struct ExpressionState
    {
        public string Text;
        public double Value;
        public bool   HasValue;
    }

    [Header("Geração de expressões")]
    [SerializeField] private int baseMin = 1;
    [SerializeField] private int baseMax = 100;

    [Header("Turnos de desbloqueio de operadores")]
    [SerializeField] private int mulDivTurn     = 15;
    [SerializeField] private int expTurn        = 30;
    [SerializeField] private int factorialTurn  = 45;

    [Header("Progressão de complexidade")]
    [SerializeField] private int termsAt10  = 2;
    [SerializeField] private int termsAt25  = 3;
    [SerializeField] private int termsAt40  = 4;
    [SerializeField] private int termsAbove = 5;

    // ── Ponto de entrada principal ─────────────────────────────────────────────

    public (string questionText, double answerValue, ExpressionState newExpression)
        PrepareQuestion(int turnCount, ExpressionState current, int consecutiveWrong, bool resetExpression)
    {
        ExpressionState expr = current;

        // Gera base nova só se não há expressão válida ou foi pedido reset.
        // Caso contrário, mantém a expressão atual — ela já cresceu via ApplyChosenOperation.
        if (!expr.HasValue || resetExpression)
            expr = GenerateBase(turnCount);

        string question = "Quanto é: " + expr.Text + " = ?";
        return (question, expr.Value, expr);
    }

    // ── Geração de opções de operação (painel "Como complicar?") ──────────────

    public string[] GenerateOperationOptions(int turnCount, ExpressionState current)
    {
        List<string> pool = new List<string>();

        int addVal = Random.Range(1, 101);
        int subVal = Random.Range(1, 101);
        pool.Add("+" + addVal); pool.Add("+" + addVal); pool.Add("+" + addVal);
        pool.Add("-" + subVal); pool.Add("-" + subVal); pool.Add("-" + subVal);

        if (turnCount >= mulDivTurn)
        {
            int m = Random.Range(2, 16);
            int d = Random.Range(2, 16);
            pool.Add("*" + m); pool.Add("*" + m);
            pool.Add("/" + d); pool.Add("/" + d);
        }
        if (turnCount >= expTurn)
            pool.Add("^" + Random.Range(2, 4));
        if (turnCount >= factorialTurn && !current.HasValue)
            pool.Add("!");

        // Fisher-Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        List<string> chosen = new List<string>();
        for (int i = 0; i < pool.Count && chosen.Count < 3; i++)
        {
            bool dup = false;
            for (int k = 0; k < chosen.Count; k++)
                if (chosen[k] == pool[i]) { dup = true; break; }
            if (!dup) chosen.Add(pool[i]);
        }
        while (chosen.Count < 3)
            chosen.Add("+" + Random.Range(1, 101));

        return chosen.ToArray();
    }

    // ── Aplica a operação escolhida pelo jogador à expressão atual ─────────────

    public ExpressionState ApplyChosenOperation(ExpressionState current, string op)
    {
        string left       = current.Text;
        bool needsParens  = op.StartsWith("*") || op.StartsWith("/")
                         || op.StartsWith("^") || op == "!";
        if (needsParens) left = "(" + left + ")";

        ExpressionState next = new ExpressionState { HasValue = true };

        if (op.StartsWith("+"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            next.Text  = left + " + " + op.Substring(1);
            next.Value = current.Value + n;
        }
        else if (op.StartsWith("-"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            next.Text  = left + " - " + op.Substring(1);
            next.Value = current.Value - n;
        }
        else if (op.StartsWith("*"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            next.Text  = left + " * " + op.Substring(1);
            next.Value = current.Value * n;
        }
        else if (op.StartsWith("/"))
        {
            double n = double.Parse(op.Substring(1), CultureInfo.InvariantCulture);
            if (System.Math.Abs(n) < 0.0001) n = 1;
            next.Text  = left + " / " + op.Substring(1);
            next.Value = current.Value / n;
        }
        else if (op.StartsWith("^"))
        {
            int exp   = int.Parse(op.Substring(1));
            next.Text  = left + " ^ " + exp;
            next.Value = System.Math.Pow(current.Value, exp);
        }
        else if (op == "!")
        {
            int n     = Mathf.Clamp(Mathf.RoundToInt((float)current.Value), 0, 7);
            next.Text  = left + "!";
            next.Value = Factorial(n);
        }

        return next;
    }

    // ── Formatação de resposta ────────────────────────────────────────────────

    public static string FormatAnswer(double value)
    {
        if (System.Math.Abs(value - System.Math.Round(value)) < 0.0001)
            return ((long)System.Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        return value.ToString("F2", CultureInfo.InvariantCulture);
    }

    // ── Privados ──────────────────────────────────────────────────────────────

    private bool ShouldGenerateNew(int turnCount, int consecutiveWrong, ExpressionState expr)
    {
        if (!expr.HasValue)        return true;
        if (consecutiveWrong >= 3) return true;
        int maxTerms = turnCount < 10 ? termsAt10
                     : turnCount < 25 ? termsAt25
                     : turnCount < 40 ? termsAt40
                     : termsAbove;
        return CountTerms(expr.Text) >= maxTerms;
    }

    private static int CountTerms(string text)
    {
        // "A + B * C" → 3 termos. Cada operador binário adiciona 1 termo.
        return text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Length / 2 + 1;
    }

    private ExpressionState GenerateBase(int turnCount)
    {
        if (turnCount < 10) return SimpleNumber();

        int roll = Random.Range(0, 10);
        if (roll < 5)                                 return SimpleNumber();
        if (roll < 7)                                 return TrigTerm(turnCount);
        if (roll < 9 && turnCount >= factorialTurn)   return FactorialTerm();
        return SimpleNumber();
    }

    private ExpressionState SimpleNumber()
    {
        int n = Random.Range(baseMin, baseMax + 1);
        return new ExpressionState { Text = n.ToString(), Value = n, HasValue = true };
    }

    private ExpressionState TrigTerm(int turnCount)
    {
        string[] fns = turnCount >= 25
            ? new[] { "sin", "cos", "tan", "sqrt" }
            : new[] { "sin", "cos" };
        string fn  = fns[Random.Range(0, fns.Length)];
        int angle  = Random.Range(0, 7) * 30;
        double val = ComputeTrig(fn, angle);
        return new ExpressionState
        {
            Text     = fn + "(" + angle + "°)",
            Value    = val,
            HasValue = true
        };
    }

    private ExpressionState FactorialTerm()
    {
        int n = Random.Range(1, 6);
        return new ExpressionState { Text = n + "!", Value = Factorial(n), HasValue = true };
    }

    private ExpressionState AppendTerm(ExpressionState current, int turnCount)
    {
        string op    = PickOperator(turnCount);
        int operand  = Random.Range(baseMin, baseMax + 1);
        return new ExpressionState
        {
            Text     = current.Text + " " + op + " " + operand,
            Value    = ApplySimpleOp(current.Value, op, operand),
            HasValue = true
        };
    }

    private string PickOperator(int turnCount)
    {
        List<string> ops = new List<string> { "+", "+", "+", "-", "-", "-" };
        if (turnCount >= mulDivTurn) { ops.Add("*"); ops.Add("/"); }
        return ops[Random.Range(0, ops.Count)];
    }

    private static double ApplySimpleOp(double a, string op, double b)
    {
        switch (op)
        {
            case "+": return a + b;
            case "-": return a - b;
            case "*": return a * b;
            case "/": return System.Math.Abs(b) < 0.0001 ? a : a / b;
            default:  return a;
        }
    }

    private static double ComputeTrig(string fn, int degrees)
    {
        double r = degrees * System.Math.PI / 180.0;
        switch (fn)
        {
            case "sin":  return System.Math.Sin(r);
            case "cos":  return System.Math.Cos(r);
            case "tan":  return System.Math.Tan(r);
            case "sqrt": return System.Math.Sqrt(System.Math.Abs(r));
            default:     return 0;
        }
    }

    private static long Factorial(int n)
    {
        long r = 1;
        for (int i = 2; i <= n; i++) r *= i;
        return r;
    }
}
