using System;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.ErrorCode;
using Microsoft.Deployment.Common.Helpers;

using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class GetContinuousExports : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");

            var AIInstance = request.DataStore.GetJson("SelectedAppInsightsInstance");
            string AIInstanceName = AIInstance["name"].ToString();
            string AIResourceGroup = ParseResourceGroupFromId(AIInstance["id"].ToString());
            if (string.IsNullOrEmpty(AIResourceGroup))
            {
                return new ActionResponse(ActionStatus.Failure, AIInstance, null, DefaultErrorCodes.DefaultErrorCode, 
                    "BadResourceGroupInAppInsightsId");
            }

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, AIResourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Get, 
                $"providers/Microsoft.Insights/components/{AIInstanceName}/exportconfiguration", "2015-05-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var continuousExports = JArray.Parse(await response.Content.ReadAsStringAsync());
                return new ActionResponse(ActionStatus.Success, continuousExports, true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new ActionResponse(ActionStatus.Failure, error, null, DefaultErrorCodes.DefaultErrorCode, "GetContinuousExports");
        }

        private string ParseResourceGroupFromId(string appInsightsId)
        {
            Match resourceGroupMatch = Regex.Match(appInsightsId, @"resourceGroups\/(.*?)\/");
            if (resourceGroupMatch.Success)
            {
                return resourceGroupMatch.Groups[1].Value;
            }

            return null;
        }
    }
}