namespace Chess.Application.Common.Authorization;

public interface IPermissionChecker
{
    bool IsUser(Guid userId);
    bool IsStaff(Guid userId);
    bool IsAdmin(Guid userId);
    bool CanBan(Guid staffId);
    bool CanPermBan(Guid staffId);
    bool CanManageRoles(Guid staffId);
    bool CanViewFullAudit(Guid staffId);
}
