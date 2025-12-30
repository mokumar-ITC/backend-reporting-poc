using System.Collections.Generic;

namespace BIEmbedSystem.Services.DTO
{
    public class FabricCapacityCreationData
    {
        /// <summary>
        /// Azure region where the Fabric Capacity should be created, e.g., "East US".
        /// </summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>
        /// SKU name for the capacity, e.g., "F2", "F4", "F8".
        /// </summary>
        public string SkuName { get; set; } = string.Empty;

        /// <summary>
        /// List of admin email addresses to assign as Fabric capacity administrators.
        /// </summary>
        public List<string> Admins { get; set; } = new();
    }

    public class FabricCapacityPatchData
    {
        /// <summary>
        /// Optional: Change the SKU name (e.g., "F4", "F8").
        /// </summary>
        public string? SkuName { get; set; }

        /// <summary>
        /// Optional: Update the Fabric capacity administrators.
        /// Provide a list of admin email addresses.
        /// </summary>
        public List<string>? Admins { get; set; }

        /// <summary>
        /// Optional: Update Azure resource tags (key-value pairs).
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }
    }
}
