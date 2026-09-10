using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EmployeeAPI.Models
{
    public class Employee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        /// <summary>Documento de identidad (ej. "12345678-9"). Opcional: el empleado también puede identificarse por Id o PIN al marcar.</summary>
        [RegularExpression(@"^\d{7,8}-[\dkK]$", ErrorMessage = "El DNI debe tener el formato 12345678-9.")]
        public string? Dni { get; set; }

        public string? Department_Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Department { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Position { get; set; } = null!;
        public string? Position_Id { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>();

            foreach (PropertyInfo propertyInfo in GetType().GetProperties())
            {
                dictionary.Add(propertyInfo.Name, propertyInfo.GetValue(this));
            }

            return dictionary;
        }
    }
}