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
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(MDMDbContext context, ILogger logger) : base(context, logger)
        {
        }
    }
}
