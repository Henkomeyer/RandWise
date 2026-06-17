namespace RandWise.UnitTests;

public class BootstrapTests
{
    [Fact]
    public void DomainAssemblyMarker_IsAvailable()
    {
        Assert.Equal("RandWise.Domain", typeof(RandWise.Domain.DomainAssemblyMarker).Assembly.GetName().Name);
    }
}
