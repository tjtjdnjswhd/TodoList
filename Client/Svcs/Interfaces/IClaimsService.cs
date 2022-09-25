using System.Security.Claims;

using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface IClaimsService
    {
        Task<ClaimsIdentity?> GetClaimsIdentityOrNullAsync();
        Task SetClaimsAsync(IEnumerable<ClaimDto> claims);
        Task RemoveClaimsAsync();
    }
}
