using EmployeeAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmployeeAPI.Tests;

public class TokenTests
{
    private const string SecretKey = "clave-de-pruebas-de-32-caracteres!";

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        var user = new User { Id = "652d1f0b4b5c9a0001a1b2c3", username = "admin", password = "admin" };

        var result = Tools.generateSecurityTokenDescriptor(SecretKey, user, expirationHours: 12);

        Assert.False(string.IsNullOrWhiteSpace(result.token));
        Assert.True(Tools.IsTokenValid(result.token!, SecretKey));
    }

    [Fact]
    public void IsTokenValid_WithWrongSecret_ReturnsFalse()
    {
        var user = new User { username = "admin", password = "admin" };
        var token = Tools.generateSecurityTokenDescriptor(SecretKey, user).token!;

        Assert.False(Tools.IsTokenValid(token, "otra-clave-distinta-de-32-caract!"));
    }

    [Fact]
    public void IsTokenValid_WithExpiredToken_ReturnsFalse()
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));
        var expiredToken = handler.WriteToken(handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Expires = DateTime.UtcNow.AddHours(-1),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        }));

        Assert.False(Tools.IsTokenValid(expiredToken, SecretKey));
    }

    [Fact]
    public void GetExpirationUtc_MatchesConfiguredExpiration()
    {
        var user = new User { username = "admin", password = "admin" };
        var token = Tools.generateSecurityTokenDescriptor(SecretKey, user, expirationHours: 3).token!;

        var expiration = Tools.GetExpirationUtc(token);

        Assert.InRange(expiration, DateTime.UtcNow.AddHours(2.9), DateTime.UtcNow.AddHours(3.1));
    }

    [Fact]
    public void GenerateToken_SetsTokenExpirationAsFullDateTime_NotJustTimeOfDay()
    {
        var user = new User { username = "admin", password = "admin" };

        var result = Tools.generateSecurityTokenDescriptor(SecretKey, user, expirationHours: 12);

        // Antes del fix, tokenExpiration era un TimeSpan derivado de TimeOfDay (perdia la fecha).
        // Ahora debe caer en un rango de fecha/hora real, ~12 horas en el futuro.
        Assert.InRange(result.tokenExpiration, DateTime.UtcNow.AddHours(11.9), DateTime.UtcNow.AddHours(12.1));
    }

    [Fact]
    public void GenerateToken_TokenExpirationMatchesJwtExpiration()
    {
        var user = new User { username = "admin", password = "admin" };

        var result = Tools.generateSecurityTokenDescriptor(SecretKey, user, expirationHours: 5);
        var jwtExpiration = Tools.GetExpirationUtc(result.token!);

        // tokenExpiration (persistido en el usuario) debe coincidir con lo que realmente dice el JWT.
        Assert.True(Math.Abs((jwtExpiration - result.tokenExpiration).TotalSeconds) < 1);
    }
}