using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface IUserRoleService
    {
        string CurrentUserName { get; set; }
        UserRoleDTO CurrentUserRole { get; set; }

        Task<UserRoleDTO> GetUserRoleAsync(string userName);
        Task<bool> HasAccessAsync(string userName, int requiredLevel);
    }
}
