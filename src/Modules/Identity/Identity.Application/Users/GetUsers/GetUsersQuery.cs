using Identity.Application.Common.Mappers;
using Identity.Domain.Repositories;
using MediatR;
using SharedKernel.Primitives;

namespace Identity.Application.Users.GetUsers;

public sealed record GetUsersQuery(int Page, int PageSize)
    : IRequest<Result<PagedList<GetUsersOutputDto>>>;

public sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, Result<PagedList<GetUsersOutputDto>>> {
    private readonly IUserRepository _userRepository;

    public GetUsersQueryHandler(IUserRepository userRepository) {
        _userRepository = userRepository;
    }

    public async Task<Result<PagedList<GetUsersOutputDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken) {
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            keyword: null,
            role: null,
            page: request.Page,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        var dtos = items.Select(UserMapper.ToGetUsersOutputDto).ToList();

        return Result.Success(PagedList<GetUsersOutputDto>.Create(
            dtos, request.Page, request.PageSize, totalCount));
    }
}