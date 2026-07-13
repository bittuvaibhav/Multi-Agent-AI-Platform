using System.ComponentModel;
using System.Globalization;
using Enterprise.Agent.Core.Abstractions.Tools;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// A deterministic calculator plugin. Exposes basic arithmetic plus a general expression
/// evaluator implemented with a shunting-yard parser (no external dependencies).
/// </summary>
public sealed class CalculatorTool : IKernelPluginSource
{
    public const string PluginName = "Calculator";

    [KernelFunction("add"), Description("Adds two numbers and returns the sum.")]
    public double Add(
        [Description("First addend.")] double a,
        [Description("Second addend.")] double b) => a + b;

    [KernelFunction("subtract"), Description("Subtracts b from a.")]
    public double Subtract(double a, double b) => a - b;

    [KernelFunction("multiply"), Description("Multiplies two numbers.")]
    public double Multiply(double a, double b) => a * b;

    [KernelFunction("divide"), Description("Divides a by b. Throws on division by zero.")]
    public double Divide(double a, double b) =>
        b == 0 ? throw new DivideByZeroException("Cannot divide by zero.") : a / b;

    [KernelFunction("evaluate"), Description("Evaluates an arithmetic expression, e.g. '3 + 4 * (2 - 1)'.")]
    public string Evaluate([Description("The arithmetic expression to evaluate.")] string expression)
    {
        var result = EvaluateExpression(expression);
        return result.ToString(CultureInfo.InvariantCulture);
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);

    /// <summary>Evaluates a arithmetic expression supporting + - * / and parentheses.</summary>
    public static double EvaluateExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression is empty.", nameof(expression));
        }

        var output = new Queue<string>();
        var operators = new Stack<char>();
        var tokens = Tokenize(expression);

        foreach (var token in tokens)
        {
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                output.Enqueue(token);
            }
            else if (token.Length == 1 && "+-*/".Contains(token[0]))
            {
                var op = token[0];
                while (operators.Count > 0 && operators.Peek() != '('
                       && Precedence(operators.Peek()) >= Precedence(op))
                {
                    output.Enqueue(operators.Pop().ToString());
                }

                operators.Push(op);
            }
            else if (token == "(")
            {
                operators.Push('(');
            }
            else if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != '(')
                {
                    output.Enqueue(operators.Pop().ToString());
                }

                if (operators.Count == 0)
                {
                    throw new FormatException("Mismatched parentheses.");
                }

                operators.Pop();
            }
            else
            {
                throw new FormatException($"Unexpected token '{token}'.");
            }
        }

        while (operators.Count > 0)
        {
            var op = operators.Pop();
            if (op is '(' or ')')
            {
                throw new FormatException("Mismatched parentheses.");
            }

            output.Enqueue(op.ToString());
        }

        return EvaluateRpn(output);
    }

    private static double EvaluateRpn(Queue<string> rpn)
    {
        var stack = new Stack<double>();
        foreach (var token in rpn)
        {
            if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
            {
                stack.Push(number);
                continue;
            }

            if (stack.Count < 2)
            {
                throw new FormatException("Invalid expression.");
            }

            var right = stack.Pop();
            var left = stack.Pop();
            stack.Push(token[0] switch
            {
                '+' => left + right,
                '-' => left - right,
                '*' => left * right,
                '/' => right == 0 ? throw new DivideByZeroException() : left / right,
                _ => throw new FormatException($"Unknown operator '{token}'.")
            });
        }

        return stack.Count == 1 ? stack.Pop() : throw new FormatException("Invalid expression.");
    }

    private static IEnumerable<string> Tokenize(string expression)
    {
        var i = 0;
        while (i < expression.Length)
        {
            var c = expression[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    i++;
                }

                yield return expression[start..i];
            }
            else if ("+-*/()".Contains(c))
            {
                yield return c.ToString();
                i++;
            }
            else
            {
                throw new FormatException($"Invalid character '{c}' in expression.");
            }
        }
    }

    private static int Precedence(char op) => op switch
    {
        '+' or '-' => 1,
        '*' or '/' => 2,
        _ => 0
    };
}
