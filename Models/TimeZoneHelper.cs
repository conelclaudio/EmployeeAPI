namespace EmployeeAPI.Models
{
    /// <summary>Convierte una fecha UTC a la hora local de un dispositivo, usando su Timezone (IANA, ej. "America/Santiago").</summary>
    public static class TimeZoneHelper
    {
        /// <summary>
        /// Devuelve la fecha/hora local, o null si no hay Timezone informado o no se pudo resolver
        /// (por ejemplo, si el sistema operativo no tiene la base de datos de zonas horarias instalada).
        /// Nunca lanza una excepción: un dato de conveniencia no debe poder romper el registro de una marca.
        /// </summary>
        public static DateTime? ToLocalTime(DateTime utcDateTime, string? ianaTimeZoneId)
        {
            if (string.IsNullOrWhiteSpace(ianaTimeZoneId))
            {
                return null;
            }

            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
                // ConvertTimeFromUtc exige Kind=Utc (lanza excepción con Kind=Local); si llega
                // Unspecified o Local, lo reinterpretamos como UTC en vez de fallar, porque el
                // contrato de este metodo es "utcDateTime ya está en UTC".
                var utc = utcDateTime.Kind == DateTimeKind.Utc ? utcDateTime : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return null;
            }
            catch (InvalidTimeZoneException)
            {
                return null;
            }
        }
    }
}