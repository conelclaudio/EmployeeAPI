using EmployeeAPI.Models;
using System.ComponentModel.DataAnnotations;

namespace EmployeeAPI.Tests;

public class ModelValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Punch_WithRequiredFields_IsValid()
    {
        var punch = new Punch
        {
            Device_Id = "652d1f0b4b5c9a0001a1b2c3",
            PunchType = "IN",
            Punch_Dtm = DateTime.UtcNow,
            Pin = "1234"
        };

        Assert.Empty(Validate(punch));
    }

    [Fact]
    public void Punch_WithoutDevice_IsInvalid()
    {
        var punch = new Punch { PunchType = "IN", Punch_Dtm = DateTime.UtcNow };

        Assert.Contains(Validate(punch), r => r.MemberNames.Contains(nameof(Punch.Device_Id)));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("abcd")]
    [InlineData("12345678901")]
    public void Enrollment_WithInvalidPin_IsInvalid(string pin)
    {
        var enrollment = new Enrollment { Employee_Id = "652d1f0b4b5c9a0001a1b2c3", Pin = pin };

        Assert.Contains(Validate(enrollment), r => r.MemberNames.Contains(nameof(Enrollment.Pin)));
    }

    [Fact]
    public void Device_WithoutTimezone_IsInvalid()
    {
        var device = new Device { Name = "Reloj Recepcion", Location = "Casa Matriz", Timezone = null! };

        Assert.Contains(Validate(device), r => r.MemberNames.Contains(nameof(Device.Timezone)));
    }

    [Fact]
    public void Employee_WithRequiredFields_IsValid()
    {
        var employee = new Employee
        {
            Name = "Ana Perez",
            Email = "ana.perez@example.com",
            Department = "Operaciones",
            Position = "Analista"
        };

        Assert.Empty(Validate(employee));
    }

    [Fact]
    public void Employee_WithoutName_IsInvalid()
    {
        var employee = new Employee { Name = null!, Email = "ana.perez@example.com", Department = "Operaciones", Position = "Analista" };

        Assert.Contains(Validate(employee), r => r.MemberNames.Contains(nameof(Employee.Name)));
    }

    [Theory]
    [InlineData("no-es-un-email")]
    [InlineData("falta-arroba.com")]
    [InlineData("")]
    public void Employee_WithInvalidEmail_IsInvalid(string email)
    {
        var employee = new Employee { Name = "Ana Perez", Email = email, Department = "Operaciones", Position = "Analista" };

        Assert.Contains(Validate(employee), r => r.MemberNames.Contains(nameof(Employee.Email)));
    }

    [Theory]
    [InlineData("12345678")]
    [InlineData("1234567-8-9")]
    [InlineData("abcdefgh-9")]
    public void Employee_WithInvalidDni_IsInvalid(string dni)
    {
        var employee = new Employee { Name = "Ana Perez", Email = "ana.perez@example.com", Department = "Operaciones", Position = "Analista", Dni = dni };

        Assert.Contains(Validate(employee), r => r.MemberNames.Contains(nameof(Employee.Dni)));
    }

    [Theory]
    [InlineData("12345678-9")]
    [InlineData("1234567-K")]
    [InlineData("1234567-k")]
    public void Employee_WithValidDni_IsValid(string dni)
    {
        var employee = new Employee { Name = "Ana Perez", Email = "ana.perez@example.com", Department = "Operaciones", Position = "Analista", Dni = dni };

        Assert.Empty(Validate(employee));
    }

    [Fact]
    public void Employee_WithoutDni_IsValid()
    {
        // El DNI es opcional: el empleado tambien puede identificarse por Id o PIN al marcar.
        var employee = new Employee { Name = "Ana Perez", Email = "ana.perez@example.com", Department = "Operaciones", Position = "Analista", Dni = null };

        Assert.Empty(Validate(employee));
    }

    [Fact]
    public void PatchEmployee_WithInvalidEmail_IsInvalid()
    {
        var patch = new PatchEmployee { Email = "no-es-un-email" };

        Assert.Contains(Validate(patch), r => r.MemberNames.Contains(nameof(PatchEmployee.Email)));
    }

    [Fact]
    public void PatchEmployee_WithAllFieldsNull_IsValid()
    {
        // PATCH es una actualizacion parcial: no enviar ningun campo es valido, a diferencia de POST.
        var patch = new PatchEmployee();

        Assert.Empty(Validate(patch));
    }
}