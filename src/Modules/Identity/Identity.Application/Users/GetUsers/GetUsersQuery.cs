using Identity.Application.Common.Mappers;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using MediatR;
using SharedKernel.Primitives;

namespace Identity.Application.Users.GetUsers;

// SortBy/SortDir để cuối và có default → mọi call-site cũ không sort vẫn biên dịch nguyên trạng.
public sealed record GetUsersQuery(
    string? Keyword,
    string? Role,
    int Page,
    int PageSize,
    string? SortBy = null,
    string? SortDir = null) : IRequest<Result<PagedList<GetUsersOutputDto>>>;

public sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, Result<PagedList<GetUsersOutputDto>>> {
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository) {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedList<GetUsersOutputDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken) {
        UserRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Role)
            && Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var parsed))
            roleFilter = parsed;
 
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            keyword: request.Keyword,
            role: roleFilter,
            isActive: null,
            page: request.Page,
            pageSize: request.PageSize,
            sort: new SortInput(request.SortBy, request.SortDir),
            cancellationToken: cancellationToken);
 
        var dtos = items.Select(UserMapper.ToGetUsersOutputDto).ToList();
 
        return Result.Success(PagedList<GetUsersOutputDto>.Create(
            dtos, request.Page, request.PageSize, totalCount));
    }
}