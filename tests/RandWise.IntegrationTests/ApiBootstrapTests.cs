namespace RandWise.IntegrationTests;

public class ApiBootstrapTests
{
    [Fact]
    public void ApiProgram_IsAvailable()
    {
        Assert.Equal("RandWise.Api", typeof(Program).Assembly.GetName().Name);
    }
}
