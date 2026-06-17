namespace RandWise.ArchitectureTests;

using System.Xml.Linq;

public class DependencyDirectionTests
{
    [Fact]
    public void Domain_HasNoForbiddenInfrastructureOrTransportReferences()
    {
        var referencedAssemblies = typeof(RandWise.Domain.DomainAssemblyMarker)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain(referencedAssemblies, name => name is not null && name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedAssemblies, name => name is not null && name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedAssemblies, name => name is not null && name.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplicationProject_ReferencesDomainProject()
    {
        var projectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "RandWise.Application",
            "RandWise.Application.csproj"));

        var projectReferences = XDocument
            .Load(projectPath)
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .ToArray();

        Assert.Contains(@"..\RandWise.Domain\RandWise.Domain.csproj", projectReferences);
    }
}
