using Chess.Application.Common.Authorization;
using Chess.Application.Ports;
using Chess.Domain.ValueObjects;

namespace Chess.Infrastructure.Services;

public sealed class PermissionChecker : IPermissionChecker
{
    private readonly IUserRepository _userRepository;

    public PermissionChecker(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public bool IsUser(Guid userId)
    {
        var user = _userRepository.GetByIdAsync(userId).Result;
        return user != null && user.Status == UserStatus.Active;
    }

    public bool IsStaff(Guid userId)
    {
        var user = _userRepository.GetByIdAsync(userId).Result;
        return user != null && user.Status == UserStatus.Active &&
               (user.Role == UserRole.Moderator || user.Role == UserRole.Admin);
    }

    public bool IsAdmin(Guid userId)
    {
        var user = _userRepository.GetByIdAsync(userId).Result;
        return user != null && user.Status == UserStatus.Active &&
               user.Role == UserRole.Admin;
    }

    public bool CanBan(Guid staffId) => IsStaff(staffId);

    public bool CanPermBan(Guid staffId) => IsAdmin(staffId);

    public bool CanManageRoles(Guid staffId) => IsAdmin(staffId);

    public bool CanViewFullAudit(Guid staffId) => IsAdmin(staffId);
}
