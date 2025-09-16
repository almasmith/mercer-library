using System;
using FluentAssertions;
using Library.Api.Dtos;
using Library.Api.Validation;

namespace Library.Tests.Validation;

public sealed class UpdateBookRequestValidatorTests
{
    private static UpdateBookRequest CreateValidRequest()
        => new UpdateBookRequest(
            Title: "Clean Architecture",
            Author: "Robert C. Martin",
            Genre: "Software",
            PublishedDate: DateTimeOffset.UtcNow.AddYears(-5),
            Rating: 4);

    [Fact]
    public void Valid_request_should_be_valid()
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest();

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_is_required(string? title)
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest() with { Title = title! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Title));
    }

    [Fact]
    public void Title_trimmed_length_over_200_is_invalid()
    {
        var validator = new UpdateBookRequestValidator();
        var tooLong = new string('x', 201);
        var request = CreateValidRequest() with { Title = $"  {tooLong}  " };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Author_is_required(string? author)
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest() with { Author = author! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Author));
    }

    [Fact]
    public void Author_trimmed_length_over_200_is_invalid()
    {
        var validator = new UpdateBookRequestValidator();
        var tooLong = new string('x', 201);
        var request = CreateValidRequest() with { Author = $"{tooLong}   " };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Author));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Genre_is_required(string? genre)
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest() with { Genre = genre! };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Genre));
    }

    [Fact]
    public void Genre_trimmed_length_over_100_is_invalid()
    {
        var validator = new UpdateBookRequestValidator();
        var tooLong = new string('x', 101);
        var request = CreateValidRequest() with { Genre = $" {tooLong} " };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Genre));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Rating_is_bounded_between_1_and_5_inclusive(int rating, bool expectedValid)
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest() with { Rating = rating };

        var result = validator.Validate(request);

        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.Rating));
        }
    }

    [Fact]
    public void PublishedDate_more_than_one_day_in_future_is_invalid()
    {
        var validator = new UpdateBookRequestValidator();
        var request = CreateValidRequest() with { PublishedDate = DateTimeOffset.UtcNow.AddDays(2) };

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateBookRequest.PublishedDate));
    }

    [Fact]
    public void PublishedDate_today_or_tomorrow_is_valid()
    {
        var validator = new UpdateBookRequestValidator();
        var today = CreateValidRequest() with { PublishedDate = DateTimeOffset.UtcNow };
        var tomorrow = CreateValidRequest() with { PublishedDate = DateTimeOffset.UtcNow.AddDays(1) };

        validator.Validate(today).IsValid.Should().BeTrue();
        validator.Validate(tomorrow).IsValid.Should().BeTrue();
    }
}


