using FluentValidation;

namespace Identity.Application.Users.ImportUsersCsv;

public sealed class ImportUsersCsvCommandValidator : AbstractValidator<ImportUsersCsvCommand> {
    // Giới hạn 5MB cho file CSV
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ImportUsersCsvCommandValidator() {
        RuleFor(x => x.File)
            .NotNull().WithMessage("File CSV không được để trống.")
            .Must(f => f.Length > 0).WithMessage("File CSV không được rỗng.")
            .Must(f => f.Length <= MaxFileSizeBytes)
            .WithMessage($"File CSV không được vượt quá {MaxFileSizeBytes / 1024 / 1024}MB.")
            .Must(f => f.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Chỉ chấp nhận file định dạng .csv.");
    }
}