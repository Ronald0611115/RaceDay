 using System.ComponentModel.DataAnnotations;
using RaceDay.API.Models;

namespace RaceDay.API.Tests;

public class RegistrationTests
{
    [Fact]
    public void ValidRegistration_ShouldPassValidation()
    {
        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Mokoena",
            Email = "thabo@example.com",
            Password = "SecurePass123",
            Role = "Participant"
        };

        var errors = ValidateRequest(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidEmail_ShouldFailValidation()
    {
        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Mokoena",
            Email = "not-an-email",
            Password = "SecurePass123",
            Role = "Participant"
        };

        var errors = ValidateRequest(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterRequest.Email)));
    }

    [Fact]
    public void ShortPassword_ShouldFailValidation()
    {
        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Mokoena",
            Email = "thabo@example.com",
            Password = "short",
            Role = "Participant"
        };

        var errors = ValidateRequest(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterRequest.Password)));
    }

    [Fact]
    public void InvalidRole_ShouldFailValidation()
    {
        var request = new RegisterRequest
        {
            FirstName = "Thabo",
            LastName = "Mokoena",
            Email = "thabo@example.com",
            Password = "SecurePass123",
            Role = "Administrator"
        };

        var errors = ValidateRequest(request);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(RegisterRequest.Role)));
    }

    private static List<ValidationResult> ValidateRequest(
        RegisterRequest request)
    {
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            errors,
            validateAllProperties: true);

        return errors;
    }
}
