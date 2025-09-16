using System;
using FluentValidation;
using Library.Api.Dtos;

namespace Library.Api.Validation;

public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("'Title' must not be empty.")
            .Must(s => (s ?? string.Empty).Trim().Length <= 200)
            .WithMessage("The length of 'Title' must be 200 characters or fewer.");

        RuleFor(x => x.Author)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("'Author' must not be empty.")
            .Must(s => (s ?? string.Empty).Trim().Length <= 200)
            .WithMessage("The length of 'Author' must be 200 characters or fewer.");

        RuleFor(x => x.Genre)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("'Genre' must not be empty.")
            .Must(s => (s ?? string.Empty).Trim().Length <= 100)
            .WithMessage("The length of 'Genre' must be 100 characters or fewer.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.PublishedDate)
            .Must(d => d <= DateTimeOffset.UtcNow.AddDays(1))
            .WithMessage("PublishedDate cannot be in the future.");
    }
}


