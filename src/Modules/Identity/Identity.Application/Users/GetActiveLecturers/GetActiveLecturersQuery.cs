using Identity.Application.Common.Mappers;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace Identity.Application.Users.GetActiveLecturers;

public sealed record GetActiveLecturersQuery(
    string? Keyword,
    int Page,
    int PageSize) : IRequest<Result<PagedList<LecturerOptionDto>>>;

public sealed class GetActiveLecturersQueryHandler
    : IRequestHandler<GetActiveLecturersQuery, Result<PagedList<LecturerOptionDto>>> {
    private readonly IUserRepository _userRepository;

    public GetActiveLecturersQueryHandler(IUserRepository userRepository) {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedList<LecturerOptionDto>>> Handle(
        GetActiveLecturersQuery request,
        CancellationToken cancellationToken) {
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            keyword: request.Keyword,
            role: UserRole.Lecturer,
            isActive: true,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var dtos = items.Select(UserMapper.ToLecturerOptionDto).ToList();

        return Result.Success(PagedList<LecturerOptionDto>.Create(
            dtos, request.Page, request.PageSize, totalCount));
    }
}