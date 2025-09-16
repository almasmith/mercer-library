using FluentAssertions;
using Library.Api.Dtos.Auth;
using Library.Api.Validation.Auth;

namespace Library.Tests.Validation.Auth;

public sealed class RegisterRequestValidatorTests
{
    private static RegisterRequest CreateValid() => new RegisterRequest("user@example.com", "12345678");

    [Fact]
    public void Valid_request_should_be_valid()
    {
        var validator = new RegisterRequestValidator();
        var request = CreateValid();

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Email_is_required(string? email)
    {
        var validator = new RegisterRequestValidator();
        var request = CreateValid() with { Email = email! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("@example.com")]
    public void Email_must_be_valid_format(string email)
    {
        var validator = new RegisterRequestValidator();
        var request = CreateValid() with { Email = email };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Password_is_required(string? password)
    {
        var validator = new RegisterRequestValidator();
        var request = CreateValid() with { Password = password! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData("abcdefgh", true)]
    public void Password_min_length_eight(string password, bool expectedValid)
    {
        var validator = new RegisterRequestValidator();
        var request = CreateValid() with { Password = password };

        var result = validator.Validate(request);

        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
        }
    }
}


