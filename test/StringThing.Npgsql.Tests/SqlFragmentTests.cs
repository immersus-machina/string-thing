using StringThing.Npgsql;
using Xunit;

namespace StringThing.Npgsql.Tests;

public class SqlFragmentTests
{
    [Fact]
    public void Dispose_WhenCalledTwice_DoesNotThrow()
    {
        // Arrange
        SqlFragment fragment = $"id = {42}";

        // Act
        fragment.Dispose();
        fragment.Dispose();
    }
}
