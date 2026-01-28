using BIEmbedSystem.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class SilverLayerService
    {
        private readonly ILogger<SilverLayerService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public SilverLayerService(ILogger<SilverLayerService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GetHeaderInfo()
        {
            return "he";
        }
    }
}
