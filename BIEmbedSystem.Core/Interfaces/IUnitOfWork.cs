using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProjectRepository Projects { get; }
        IPBIGroupWorkspaceReportRepository GroupWorkspaceReports { get; }
        IPBIMenubarByGroupRepository MenubarByGroups { get; }
        IPBIWorkspaceReportRepository WorkspaceReports { get; }         
        IPBINavigationManagementRepository NavigationManagements { get; }
        IPBINavigationUserAccessRepository NavigationUserAccess { get; }

        ICapacitySchedulerModel CapacityScheduler { get; }
        Task<int> CompletedAsync();
    }
}
