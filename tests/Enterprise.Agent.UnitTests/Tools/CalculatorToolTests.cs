using Enterprise.Agent.Tools;
using Xunit;

namespace Enterprise.Agent.UnitTests.Tools;

public sealed class CalculatorToolTests
{
    [Theory]
    [InlineData("2 + 3", 5)]
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("3 + 4 * (2 - 1)", 7)]
    [InlineData("-not-parsed", 0)] // guarded below
    public void EvaluateExpression_ComputesCorrectly(string expression, double expected)
    {
        if (expression == "-not-parsed")
        {
            Assert.Throws<FormatException>(() => CalculatorTool.EvaluateExpression(expression));
            return;
        }

        var result = CalculatorTool.EvaluateExpression(expression);
        Assert.Equal(expected, result, precision: 6);
    }

    [Fact]
    public void EvaluateExpression_DivisionByZero_Throws()
    {
        Assert.Throws<DivideByZeroException>(() => CalculatorTool.EvaluateExpression("1 / 0"));
    }

    [Fact]
    public void Add_ReturnsSum()
    {
        var tool = new CalculatorTool();
        Assert.Equal(7, tool.Add(3, 4));
    }

    [Fact]
    public void BuildPlugin_ExposesFunctions()
    {
        var plugin = new CalculatorTool().BuildPlugin();
        Assert.Equal(CalculatorTool.PluginName, plugin.Name);
        Assert.Contains(plugin, f => f.Name == "evaluate");
    }
}
