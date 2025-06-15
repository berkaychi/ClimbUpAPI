using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace ClimbUpAPI.Services
{
    public interface IAccountManagementService
    {
        Task<IdentityResult> ConfirmEmailChangeAsync(string userId, string token);
        Task<IdentityResult> InitiateAccountDeletionAsync(string userId);
        Task<(IdentityResult result, string? userName, string? email)> ConfirmAccountDeletionAsync(string token);
        Task<IdentityResult> ResendAccountDeletionEmailAsync(string userId);
    }
}