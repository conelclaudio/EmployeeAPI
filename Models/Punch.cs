using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAPI.Models
{
    /// <summary>Marca de asistencia registrada en un dispositivo.</summary>
    public class Punch
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Device_Id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        public string? Employee_Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? PunchType_Id { get; set; }

        /// <summary>Código del tipo de marca (IN, OUT, BREAK_IN, BREAK_OUT).</summary>
        [Required]
        [StringLength(20, MinimumLength = 2)]
        public string PunchType { get; set; } = null!;

        /// <summary>Fecha y hora de la marca en UTC. Si no se informa, el servidor usa la hora de recepción.</summary>
        public DateTime Punch_Dtm { get; set; }

        /// <summary>Zona horaria informada por el dispositivo al momento de la marca.</summary>
        [StringLength(64)]
        public string? Timezone { get; set; }

        /// <summary>Hora local de la marca, calculada a partir de Punch_Dtm (UTC) y Timezone. Null si no hay Timezone o no se pudo resolver. No se persiste: se recalcula cada vez.</summary>
        [BsonIgnore]
        public DateTime? Punch_Dtm_Local => TimeZoneHelper.ToLocalTime(Punch_Dtm, Timezone);

        [StringLength(20)]
        public string? Dni { get; set; }

        [RegularExpression("^[0-9]{4,10}$", ErrorMessage = "El PIN debe contener entre 4 y 10 dígitos.")]
        public string? Pin { get; set; }

        /// <summary>Estado del procesamiento: PENDING, VALID o REJECTED.</summary>
        public string Status { get; set; } = PunchStatus.Pending;
    }

    public static class PunchStatus
    {
        public const string Pending = "PENDING";
        public const string Valid = "VALID";
        public const string Rejected = "REJECTED";
    }
}