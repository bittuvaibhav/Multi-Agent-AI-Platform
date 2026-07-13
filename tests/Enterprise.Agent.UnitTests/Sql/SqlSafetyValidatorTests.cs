using Enterprise.Agent.Infrastructure.Sql;
using Enterprise.Agent.Models.Enums;
using Xunit;

namespace Enterprise.Agent.UnitTests.Sql;

public sealed class SqlSafetyValidatorTests
{
    private readonly SqlSafetyValidator _validator = new();

    [Fact]
    public void Select_IsAllowed()
    {
        var result = _validator.Validate("SELECT id, name FROM Customers WHERE active = 1");
        Assert.True(result.IsAllowed);
        Assert.Equal(SqlStatementKind.Select, result.Kind);
    }

    [Fact]
    public void Cte_Select_IsAllowed()
    {
        var result = _validator.Validate("WITH c AS (SELECT * FROM Orders) SELECT * FROM c");
        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData("DELETE FROM Customers")]
    [InlineData("UPDATE Customers SET name = 'x'")]
    [InlineData("DROP TABLE Customers")]
    [InlineData("TRUNCATE TABLE Customers")]
    [InlineData("INSERT INTO Customers (id) VALUES (1)")]
    [InlineData("EXEC sp_who")]
    public void DestructiveStatements_AreRejected(string sql)
    {
        var result = _validator.Validate(sql);
        Assert.False(result.IsAllowed);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public void StackedStatements_AreRejected()
    {
        var result = _validator.Validate("SELECT 1; DROP TABLE Customers");
        Assert.False(result.IsAllowed);
    }

    [Fact]
    public void SelectInto_IsRejected()
    {
        var result = _validator.Validate("SELECT * INTO Backup FROM Customers");
        Assert.False(result.IsAllowed);
    }
}
