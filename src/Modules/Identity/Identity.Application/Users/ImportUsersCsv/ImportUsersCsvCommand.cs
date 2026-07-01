using CsvHelper;
using CsvHelper.Configuration;
using Identity.Domain.Entities;
using Identity.Domain.Errors;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Primitives;
using System.Globalization;

namespace Identity.Application.Users.ImportUsersCsv;

public sealed record ImportUsersCsvCommand(IFormFile File)
    : IRequest<Result<ImportUsersCsvOutputDto>>;

public sealed class ImportUsersCsvCommandHandler
    : IRequestHandler<ImportUsersCsvCommand, Result<ImportUsersCsvOutputDto>> {
    // Giới hạn số dòng tối đa để tránh quá tải — không tính header row.
    private const int MaxRowCount = 500;

    private readonly UserManager<AppUser> _userManager;
    private readonly IUserRepository _userRepository;
    private readonly IPublisher _publisher;

    public ImportUsersCsvCommandHandler(
        UserManager<AppUser> userManager,
        IUserRepository userRepository,
        IPublisher publisher) {
        _userManager = userManager;
        _userRepository = userRepository;
        _publisher = publisher;
    }

    public async Task<Result<ImportUsersCsvOutputDto>> Handle(
        ImportUsersCsvCommand request,
        CancellationToken cancellationToken) {
        var errors = new List<CsvRowError>();
        var successCount = 0;

        using var stream = request.File.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) {
            HasHeaderRecord = true,
            MissingFieldFound = null,    // bỏ qua field thiếu — validate thủ công bên dưới
            HeaderValidated = null       // không throw khi header không khớp hoàn toàn
        });

        var records = new List<CsvUserRow>();

        try {
            await csv.ReadAsync();
            csv.ReadHeader();

            var rowNumber = 1; 

            while (await csv.ReadAsync()) {
                rowNumber++;

                if (rowNumber - 1 > MaxRowCount) {
                    errors.Add(new CsvRowError(rowNumber,
                        $"File vượt quá giới hạn {MaxRowCount} dòng."));
                    break;
                }

                records.Add(new CsvUserRow(
                    RowNumber: rowNumber,
                    FullName: csv.GetField("FullName"),
                    Email: csv.GetField("Email"),
                    Role: csv.GetField("Role"),
                    Password: csv.GetField("Password")));
            }
        }
        catch (Exception) {
            return Result.Failure<ImportUsersCsvOutputDto>(UserErrors.CsvInvalidFormat);
        }

        foreach (var row in records) {
            var validationError = ValidateRow(row);
            if (validationError is not null) {
                errors.Add(new CsvRowError(row.RowNumber, validationError));
                continue;
            }

            var role = Enum.Parse<UserRole>(row.Role!, ignoreCase: true);

            // Email uniqueness check per row
            var emailExists = await _userRepository.ExistsByEmailAsync(
                row.Email!, cancellationToken: cancellationToken);

            if (emailExists) {
                errors.Add(new CsvRowError(row.RowNumber, "Email đã tồn tại trong hệ thống."));
                continue;
            }

            var createResult = AppUser.Create(
                id: Guid.NewGuid(),
                email: row.Email!,
                fullName: row.FullName!,
                role: role);

            if (createResult.IsFailure) {
                errors.Add(new CsvRowError(row.RowNumber, createResult.Error.Message));
                continue;
            }

            var user = createResult.Value;
            var identityResult = await _userManager.CreateAsync(user, row.Password!);

            if (!identityResult.Succeeded) {
                var reason = identityResult.Errors.FirstOrDefault()?.Description
                    ?? "Tạo tài khoản thất bại.";
                errors.Add(new CsvRowError(row.RowNumber, reason));
                continue;
            }

            await _userManager.AddToRoleAsync(user, role.ToString());

            foreach (var domainEvent in user.DomainEvents)
                await _publisher.Publish(domainEvent, cancellationToken);

            user.ClearDomainEvents();
            successCount++;
        }

        return Result.Success(new ImportUsersCsvOutputDto(
            SuccessCount: successCount,
            FailureCount: errors.Count,
            Errors: errors));
    }

    // ── Private helpers

    private static string? ValidateRow(CsvUserRow row) {
        if (string.IsNullOrWhiteSpace(row.FullName))
            return "FullName không được để trống.";

        if (string.IsNullOrWhiteSpace(row.Email))
            return "Email không được để trống.";

        if (!IsValidEmail(row.Email))
            return "Email không hợp lệ.";

        if (string.IsNullOrWhiteSpace(row.Role))
            return "Role không được để trống.";

        if (!Enum.TryParse<UserRole>(row.Role, ignoreCase: true, out _))
            return "Role không hợp lệ. Chỉ chấp nhận: Admin, Lecturer, Student.";

        if (string.IsNullOrWhiteSpace(row.Password))
            return "Password không được để trống.";

        return null;
    }

    private static bool IsValidEmail(string email) {
        try {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch {
            return false;
        }
    }

    private sealed record CsvUserRow(
        int RowNumber,
        string? FullName,
        string? Email,
        string? Role,
        string? Password);
}