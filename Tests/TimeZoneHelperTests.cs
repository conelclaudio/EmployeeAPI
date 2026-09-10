using EmployeeAPI.Models;

namespace EmployeeAPI.Tests;

public class TimeZoneHelperTests
{
    [Fact]
    public void ToLocalTime_WithBogotaTimezone_SubtractsFiveHours()
    {
        // America/Bogota no tiene horario de verano: UTC-5 todo el año, ideal para un test estable.
        var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var local = TimeZoneHelper.ToLocalTime(utc, "America/Bogota");

        Assert.Equal(new DateTime(2026, 6, 15, 7, 0, 0), local);
    }

    [Fact]
    public void ToLocalTime_WithTokyoTimezone_AddsNineHours()
    {
        // Asia/Tokyo tampoco tiene horario de verano: UTC+9 todo el año.
        var utc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var local = TimeZoneHelper.ToLocalTime(utc, "Asia/Tokyo");

        Assert.Equal(new DateTime(2026, 6, 15, 21, 0, 0), local);
    }

    [Fact]
    public void ToLocalTime_WithNullTimezone_ReturnsNull()
    {
        var local = TimeZoneHelper.ToLocalTime(DateTime.UtcNow, null);

        Assert.Null(local);
    }

    [Fact]
    public void ToLocalTime_WithEmptyTimezone_ReturnsNull()
    {
        var local = TimeZoneHelper.ToLocalTime(DateTime.UtcNow, "");

        Assert.Null(local);
    }

    [Fact]
    public void ToLocalTime_WithInvalidTimezoneId_ReturnsNullInsteadOfThrowing()
    {
        var local = TimeZoneHelper.ToLocalTime(DateTime.UtcNow, "Esto/NoExiste");

        Assert.Null(local);
    }

    [Fact]
    public void Punch_Dtm_Local_ReflectsTimezoneConversion()
    {
        var punch = new Punch
        {
            Device_Id = "652d1f0b4b5c9a0001a1b2c3",
            PunchType = "IN",
            Punch_Dtm = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            Timezone = "America/Bogota"
        };

        Assert.Equal(new DateTime(2026, 6, 15, 7, 0, 0), punch.Punch_Dtm_Local);
    }

    [Fact]
    public void Punch_Dtm_Local_WithoutTimezone_IsNull()
    {
        var punch = new Punch
        {
            Device_Id = "652d1f0b4b5c9a0001a1b2c3",
            PunchType = "IN",
            Punch_Dtm = DateTime.UtcNow,
            Timezone = null
        };

        Assert.Null(punch.Punch_Dtm_Local);
    }
}