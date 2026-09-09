using EmployeeAPI.Models;

namespace EmployeeAPI.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("admin");

        Assert.True(PasswordHasher.Verify("admin", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("admin");

        Assert.False(PasswordHasher.Verify("otra-clave", hash));
    }

    [Fact]
    public void Hash_SamePassword_ProducesDifferentHashesEachTime()
    {
        var hash1 = PasswordHasher.Hash("admin");
        var hash2 = PasswordHasher.Hash("admin");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_NeverStoresThePlainTextPassword()
    {
        var hash = PasswordHasher.Hash("admin");

        Assert.DoesNotContain("admin", hash);
    }

    [Fact]
    public void Verify_WithMalformedOrLegacyPlainTextValue_ReturnsFalseInsteadOfThrowing()
    {
        Assert.False(PasswordHasher.Verify("admin", "admin"));
    }
}