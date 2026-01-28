namespace BIEmbedSystem.Core.Entities
{
    public class AzureAdSettings
    {
        // Directory (tenant) ID of the Azure AD app
        public required string TenantId { get; set; }
        // Application (client) ID of the Azure AD app
        public required string ClientId { get; set; }
        // Client secret of the Azure AD app
        public required string ClientSecret { get; set; }
        public string? GraphScopeBase { get; set; }
        public string? ObjectId { get; set; }
        // Server address of the Fabric SQL Analytics endpoint
        public required string serverAddress { get; set; }
        // Name of the Lakehouse or Warehouse to connect to
        public required string LakeHouseName { get; set; }
        // Can be set to 'MasterUser' or 'ServicePrincipal'
        public string? AuthenticationMode { get; set; }
        // URL used for initiating authorization request
        public string? AuthorityUrl { get; set; }
        public string? Authority { get; set; }
        // ScopeBase of AAD app. Use the below configuration to use all the permissions provided in the AAD app through Azure portal.
        public string[]? ScopeBase { get; set; } = Array.Empty<string>();
        // Master user email address. Required only for MasterUser authentication mode.
        public string? PbiUsername { get; set; }
        // Master user email password. Required only for MasterUser authentication mode.
        public string? PbiPassword { get; set; }
        // Power BI API base URL WorkspaceId for which Embed token needs to be generated
        public string? WorkspaceId { get; set; }
        public string? Instance { get; set; }

    }

}
