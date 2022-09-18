using System.Security.Claims;

using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface IAuthenticationService
    {
        Task<IEnumerable<Claim>?> GetClaimsOrNullAsync();
        Task<bool> IsClaimExpiredAsync();
        Task RemoveClaimsAsync();
        Task SetClaimsAsync(IEnumerable<ClaimDto> claimDtos);
    }
}
