using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EmployeeAPI.Models
{
    public class PatchEmployee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [StringLength(100, MinimumLength = 2)]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "El email no tiene un formato válido.")]
        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(100, MinimumLength = 2)]
        public string? Department { get; set; }

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