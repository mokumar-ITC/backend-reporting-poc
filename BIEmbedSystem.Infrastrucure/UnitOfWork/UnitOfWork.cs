using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Core.Interfaces;
using BIEmbedSystem.Infrastrucure.Repository;
using Microsoft.Extensions.Logging;
using System.Data.Odbc;

namespace BIEmbedSystem.Infrastrucure.UnitOfWork
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly MDMDbContext _context;
        private readonly ILogger _logger;
        public IProjectRepository Projects { get; private set; }

        public IPBIGroupWorkspaceReportRepository GroupWorkspaceReports {  get; private set; }
        public IPBIMenubarByGroupRepository MenubarByGroups { get; private set; }

        public IPBIWorkspaceReportRepository WorkspaceReports { get; private set; }

        public IPBINavigationManagementRepository NavigationManagements { get; private set; }
        public IPBINavigationUserAccessRepository NavigationUserAccess { get; private set; }

        public ICapacitySchedulerModel CapacitySchedulerModel { get; private set; }

        public ICapacitySchedulerModel CapacityScheduler => throw new NotImplementedException();

        public UnitOfWork(
         MDMDbContext context,
         ILoggerFactory logger
         )
        {
            _context = context;
            _logger = logger.CreateLogger("logs");

            Projects = new ProjectRepository(_context, _logger);
            GroupWorkspaceReports = new PBIGroupWorkspaceReportRepository(_context, _logger);
            MenubarByGroups = new PBIMenubarByGroupRepository(_context, _logger);
            WorkspaceReports = new PBIWorkspaceReportRepository(_context, _logger);
            NavigationManagements = new PBINavigationManagementRepository(_context, _logger);
            NavigationUserAccess = new PBINavigationUserAccessRepository(_context, _logger);
            CapacitySchedulerModel = new CapacitySchedulerModelRepository(_context, _logger);
        }

        public async Task<int> CompletedAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }


}
