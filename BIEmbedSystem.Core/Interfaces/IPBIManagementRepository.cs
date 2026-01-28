using BIEmbedSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Interfaces
{
    public interface IPBIGroupWorkspaceReportRepository : IGenericRepository<PBIGroupWorkspaceReport>
    {
    }
    public interface IPBIMenubarByGroupRepository : IGenericRepository<PBIMenubarByGroup>
    {
    }
    public interface IPBIWorkspaceReportRepository : IGenericRepository<PBIWorkspaceReport>
    {
    }
    public interface IPBINavigationManagementRepository : IGenericRepository<PBINavigationManagement>
    {
    }
    public interface IPBINavigationUserAccessRepository : IGenericRepository<PBINavigationUserAccess>
    {
    }
    public interface ICapacitySchedulerModel : IGenericRepository<CapacitySchedulerModel>
    {
        
    }
}
