using BeloMercado.Application.Interfaces.Auth;

namespace BeloMercado.UnitTests.TestDoubles;

public class FakePasswordGenerator : IPasswordGenerator
{
    public string Next { get; set; } = "Temp1234!Pwd";
    public string GenerateTemporaryPassword() => Next;
}
