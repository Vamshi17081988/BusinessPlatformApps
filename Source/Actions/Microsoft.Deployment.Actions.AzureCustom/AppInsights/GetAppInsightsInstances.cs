using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.ErrorCode;
using Microsoft.Deployment.Common.Helpers;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class GetAppInsightsInstances : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId);

            var response = await client.ExecuteWithSubscriptionAsync(HttpMethod.Get, $"providers/Microsoft.Insights/components", "2015-05-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var AIInstances = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                return new ActionResponse(ActionStatus.Success, AIInstances, true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new ActionResponse(ActionStatus.Failure, error, null, DefaultErrorCodes.DefaultErrorCode, "GetAppInsightsInstances");
        }
    }
}