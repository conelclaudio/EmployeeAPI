using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace EmployeeAPI.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string username { get; set; } = null!;
        /// <summary>Hash PBKDF2 de la contraseña (ver PasswordHasher.cs), no la contraseña en texto plano.</summary>
        public string password { get; set; } = null!;
        public string? token { get; set; }
        public TimeSpan tokenExpiration { get; set; }
    }
}