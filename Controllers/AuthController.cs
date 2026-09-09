using EmployeeAPI.Models;
using EmployeeAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EmployeeAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(MongoDBService mongoDBService, IOptions<JwtSettings> jwtSettings)
        {
            _mongoDBService = mongoDBService;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("Login")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<LoginResponse>> Post(LoginRequest request)
        {
            var listUser = await _mongoDBService.GetUsersAsync();
            if (listUser is null || listUser.Count == 0)
            {
                await _mongoDBService.CreateUserAsync(new User { username = "admin", password = PasswordHasher.Hash("admin") });
            }

            var userFind = await _mongoDBService.GetUserAsync(request.Username);

            // Mismo mensaje para usuario inexistente y contraseña incorrecta, para no permitir enumerar usuarios.
            if (userFind is null || !PasswordHasher.Verify(request.Password, userFind.password))
            {
                return Unauthorized("Usuario o contraseña incorrectos.");
            }

            if (userFind.token is null || !Tools.IsTokenValid(userFind.token, _jwtSettings.SecretKey))
            {
                userFind = Tools.generateSecurityTokenDescriptor(_jwtSettings.SecretKey, userFind, _jwtSettings.ExpirationHours);
                await _mongoDBService.UpdateUserAsync(userFind.Id!, userFind);
            }

            return new LoginResponse
            {
                Username = userFind.username,
                Token = userFind.token!,
                ExpiresAtUtc = Tools.GetExpirationUtc(userFind.token!)
            };
        }
    }
}