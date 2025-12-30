//using Microsoft.PowerBI.Api;
//using Microsoft.PowerBI.Api.Models;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;


//namespace BIEmbedSystem.Services
//{
//    public class GetEmbedTokenForRDLReportV2Async
//    {
//        public async Task<EmbedToken> GetEmbedTokenForRDLReportV2Async(PowerBIClient pbiClient, Guid workspaceId, Guid reportId, string accessLevel = "View")
//        {
//            var report = await pbiClient.Reports.GetReportInGroupAsync(workspaceId, reportId);
//            Console.WriteLine($"ReportType: {report.ReportType}");
//            Console.WriteLine($"Report.DatasetId (may be empty for RDL): {report.DatasetId ?? "<null>"}");

//            var datasourcesResponse = await pbiClient.Reports.GetDatasourcesInGroupAsync(workspaceId, reportId);
//            var datasourceList = datasourcesResponse.Value ?? new List<Datasource>();

//            var guidRegex = new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}", RegexOptions.Compiled);

//            var datasetGuids = new HashSet<Guid>();
//            var targetWorkspaceGuids = new HashSet<Guid>();

//            foreach (var ds in datasourceList)
//            {
//                try
//                {
//                    // Try to get a datasource type string safely
//                    var typeStr = (ds.GetType().GetProperty("DatasourceType")?.GetValue(ds) ??
//                                   ds.GetType().GetProperty("DataSourceType")?.GetValue(ds) ??
//                                   "")?.ToString() ?? "";

//                    string connectionDetailsStr = null;
//                    var connProp = ds.GetType().GetProperty("ConnectionDetails");
//                    if (connProp != null)
//                    {
//                        var connVal = connProp.GetValue(ds);
//                        if (connVal != null) connectionDetailsStr = JsonConvert.SerializeObject(connVal);
//                    }

//                    // Look for GUIDs in connection details always (covers many SDK shapes)
//                    if (!string.IsNullOrEmpty(connectionDetailsStr))
//                    {
//                        var matches = guidRegex.Matches(connectionDetailsStr);
//                        foreach (Match m in matches)
//                        {
//                            if (Guid.TryParse(m.Value, out var g) && g != Guid.Empty) datasetGuids.Add(g);
//                        }

//                        // attempt to extract workspace id from typical path "/groups/{workspaceId}/datasets/{datasetId}"
//                        var workspaceMatch = Regex.Match(connectionDetailsStr, @"/groups/([0-9a-fA-F\-]{36})/datasets", RegexOptions.IgnoreCase);
//                        if (workspaceMatch.Success && Guid.TryParse(workspaceMatch.Groups[1].Value, out var wsGuid) && wsGuid != Guid.Empty)
//                        {
//                            targetWorkspaceGuids.Add(wsGuid);
//                        }
//                    }

//                    // If datasource object exposes an id-like property, try to read it
//                    var dsIdProp = ds.GetType().GetProperty("DatasourceId") ?? ds.GetType().GetProperty("DataSourceId");
//                    if (dsIdProp != null)
//                    {
//                        var dsIdVal = dsIdProp.GetValue(ds)?.ToString();
//                        if (!string.IsNullOrEmpty(dsIdVal))
//                        {
//                            if (Guid.TryParse(dsIdVal, out var g) && g != Guid.Empty) datasetGuids.Add(g);
//                        }
//                    }

//                    // Also, if the type string hints "PowerBI", keep it (we still rely on GUID detection)
//                    // nothing else required here
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"Warning parsing datasource: {ex.Message}");
//                }
//            }

//            // fallback: include report.DatasetId if it's a valid guid
//            if (!string.IsNullOrEmpty(report.DatasetId) && Guid.TryParse(report.DatasetId, out var repDs) && repDs != Guid.Empty)
//            {
//                datasetGuids.Add(repDs);
//            }

//            if (datasetGuids.Count > 0)
//            {
//                Console.WriteLine("Found Power BI dataset IDs referenced by RDL: " + string.Join(", ", datasetGuids));

//                // Build V2 dataset objects using reflection so we set Id as the correct CLR type (Guid or string)
//                var v2Datasets = new List<GenerateTokenRequestV2Dataset>();
//                foreach (var d in datasetGuids)
//                {
//                    // create instance
//                    var dsObj = (GenerateTokenRequestV2Dataset)Activator.CreateInstance(typeof(GenerateTokenRequestV2Dataset))!;

//                    // set Id using reflection depending on property type
//                    var idProp = dsObj.GetType().GetProperty("Id");
//                    if (idProp == null) throw new InvalidOperationException("GenerateTokenRequestV2Dataset has no Id property in this SDK version.");

//                    if (idProp.PropertyType == typeof(Guid) || idProp.PropertyType == typeof(Guid?))
//                    {
//                        idProp.SetValue(dsObj, d);
//                    }
//                    else
//                    {
//                        // fallback - set string
//                        idProp.SetValue(dsObj, d.ToString());
//                    }

//                    // set XmlaPermissions
//                    var xmlaProp = dsObj.GetType().GetProperty("XmlaPermissions");
//                    if (xmlaProp != null) xmlaProp.SetValue(dsObj, "ReadOnly");

//                    // set AllowEdit if available
//                    var allowEditProp = dsObj.GetType().GetProperty("AllowEdit");
//                    if (allowEditProp != null)
//                    {
//                        if (allowEditProp.PropertyType == typeof(bool) || allowEditProp.PropertyType == typeof(bool?))
//                            allowEditProp.SetValue(dsObj, false);
//                    }

//                    v2Datasets.Add(dsObj);
//                }

//                // Build reports list
//                var v2Reports = new List<GenerateTokenRequestV2Report> { new GenerateTokenRequestV2Report(reportId, allowEdit: false) };

//                // targetWorkspaces if discovered
//                List<GenerateTokenRequestV2TargetWorkspace>? v2TargetWorkspaces = null;
//                if (targetWorkspaceGuids.Count > 0)
//                {
//                    targetWorkspaceGuids.Add(workspaceId); // ensure report workspace included
//                    v2TargetWorkspaces = targetWorkspaceGuids.Select(ws => new GenerateTokenRequestV2TargetWorkspace(ws)).ToList();
//                }

//                var tokenRequestV2 = new GenerateTokenRequestV2(datasets: v2Datasets, reports: v2Reports, targetWorkspaces: v2TargetWorkspaces);

//                // DEBUG: serialize and log the request JSON to verify payload before sending
//                try
//                {
//                    var debugJson = JsonConvert.SerializeObject(tokenRequestV2, Formatting.Indented,
//                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
//                    Console.WriteLine("V2 token request payload:\n" + debugJson);
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine("Failed to serialize tokenRequestV2 for debug: " + ex.Message);
//                }

//                Console.WriteLine("Generating V2 embed token (paginated report with dataset(s)).");
//                var embedToken = await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequestV2);
//                return embedToken;
//            }
//            else
//            {
//                Console.WriteLine("No Power BI datasets detected for RDL. Using GenerateTokenInGroup (V1) for view.");
//                var generateTokenParams = new GenerateTokenRequest(accessLevel: accessLevel);
//                var embedToken = await pbiClient.Reports.GenerateTokenInGroupAsync(workspaceId, reportId, generateTokenParams);
//                return embedToken;
//            }
//        }

//    }
//}


