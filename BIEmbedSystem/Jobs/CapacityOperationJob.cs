using BIEmbedSystem.API.Controllers;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using System;
using System.Threading.Tasks;

namespace BIEmbedSystem.API.Jobs
{
    public class CapacityOperationJob : IJob
    {
        private readonly FabricCapacityService _capacityService;

        public CapacityOperationJob(FabricCapacityService capacityService)
        {
            _capacityService = capacityService;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var data = context.MergedJobDataMap;
            var capacityName = data.GetString("capacityName");
            var operation = data.GetString("operation");
            Console.WriteLine($"🚀 Running Capacity Job: {operation} for {capacityName}");

            if (operation == "suspend")
            {
                // call your service to suspend capacity
                // ✅ Call your operation logic here, e.g.
                await _capacityService.SuspendCapacityAsync("054afeda-f1ba-4f7a-81bb-85da2f7a1203", "rg-itc-fabric-wus", capacityName);
                Console.WriteLine($"🚀 Suspend Successfully Capacity Job: {operation} for {capacityName}");
            }
            else if (operation == "resume")
            {
                // call your service to resume capacity
                // ✅ Call your operation logic here, e.g.
                await _capacityService.ResumeCapacityAsync("054afeda-f1ba-4f7a-81bb-85da2f7a1203", "rg-itc-fabric-wus", capacityName);
                Console.WriteLine($"🚀 Resume Successfully Capacity Job: {operation} for {capacityName}");
            }


            await Task.CompletedTask;
        }
    }
}
