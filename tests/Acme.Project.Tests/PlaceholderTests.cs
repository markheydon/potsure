namespace Acme.Project.Tests;

public sealed class PlaceholderTests
{
    [Fact]
    public void Placeholder_IsTemplate_Expected()
    {
        Assert.True(Placeholder.IsTemplate);
    }
}
