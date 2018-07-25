using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using System;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class GrantSPNPermissionsToResourceGroup : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string tenant = request.DataStore.GetValue("SPNTenantId");
            string appId = request.DataStore.GetValue("SPNAppId");
            string appKey = request.DataStore.GetValue("SPNKey");
            string appObject = request.DataStore.GetValue("SPNObjectId");
            string ADLAName = request.DataStore.GetValue("ADLAName");
            string ADLSName = request.DataStore.GetValue("ADLSName");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, resourceGroup);
            var rolesResponses = await client.ExecuteWithSubscriptionAsync(HttpMethod.Get, "providers/Microsoft.Authorization/roleDefinitions", "2015-07-01", string.Empty, "$filter=roleName%20eq%20'Contributor'");
            var rolesResponsesString = await rolesResponses.Content.ReadAsStringAsync();
            var obj = JsonUtility.GetJObjectFromJsonString(rolesResponsesString);
            var roleGuid = obj["value"][0]["name"].ToString();

            JObject paramsArm = new JObject();
            JObject nestedParam = new JObject();
            nestedParam.Add("roleDefinitionId", $"/subscriptions/{subscriptionId}/providers/Microsoft.Authorization/roleDefinitions/{roleGuid}");
            nestedParam.Add("principalId", appObject);
            paramsArm.Add("properties", nestedParam);
            var rolesResponses2 = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Put, $"providers/Microsoft.Authorization/roleAssignments/{Guid.NewGuid()}", "2015-07-01", paramsArm.ToString());
            if (rolesResponses2.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Success);
            }

            return new ActionResponse(ActionStatus.Failure);

        }
    }
}