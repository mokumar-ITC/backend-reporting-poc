using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Infrastrucure.Repository
{
    public class PBIGroupWorkspaceReportRepository : GenericRepository<PBIGroupWorkspaceReport>, IPBIGroupWorkspaceReportRepository
    {
        public PBIGroupWorkspaceReportRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
    public class PBIMenubarByGroupRepository : GenericRepository<PBIMenubarByGroup>, IPBIMenubarByGroupRepository
    {
        public PBIMenubarByGroupRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
    public class PBIWorkspaceReportRepository : GenericRepository<PBIWorkspaceReport>, IPBIWorkspaceReportRepository
    {
        public PBIWorkspaceReportRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
    public class PBINavigationManagementRepository : GenericRepository<PBINavigationManagement>, IPBINavigationManagementRepository
    {
        public PBINavigationManagementRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
    public class PBINavigationUserAccessRepository : GenericRepository<PBINavigationUserAccess>, IPBINavigationUserAccessRepository
    {
        public PBINavigationUserAccessRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }

    public class CapacitySchedulerModelRepository : GenericRepository<CapacitySchedulerModel>, ICapacitySchedulerModel
    {
        public CapacitySchedulerModelRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
}
