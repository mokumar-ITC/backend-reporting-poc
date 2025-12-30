using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<bool> AssignUserAsync(CreateUserSubscriptionRequest request);
        Task<bool> UnassignUserAsync(int userSubscriptionId);
        Task<IEnumerable<UserDto>> GetUsersInOrgSubscriptionAsync(int orgSubscriptionId);
    }
}
