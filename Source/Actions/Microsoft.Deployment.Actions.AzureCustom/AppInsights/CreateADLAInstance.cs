using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.ErrorCode;
using Microsoft.Deployment.Common.Helpers;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class CreateADLAInstance : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            var AIInstance = request.DataStore.GetJson("SelectedAppInsightsInstance");
            string AILocation = AIInstance["location"].ToString();
            string adlsName = "aistorage" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8).ToLower();

            var adlsAccount = await CreateAdlsAsync(adlsName, AILocation, azureToken, resourceGroup, subscriptionId);
            if (!adlsAccount)
            {
                return new ActionResponse(ActionStatus.Failure, null, null, DefaultErrorCodes.DefaultErrorCode, "UnableToCreateADLS");
            }

            string adlaAccountName = "aianalytics" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8).ToLower();
            dynamic payload = new ExpandoObject();
            payload.location = AILocation;
            payload.properties = new ExpandoObject();
            payload.properties.defaultDataLakeStoreAccount = adlsName;
            payload.properties.dataLakeStoreAccounts = new ExpandoObject[1];
            payload.properties.dataLakeStoreAccounts[0] = new ExpandoObject();
            payload.properties.dataLakeStoreAccounts[0].name = adlsName;

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, resourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Put,
               $"providers/Microsoft.DataLakeAnalytics/accounts/{adlaAccountName}", "2016-11-01", JsonUtility.GetJsonStringFromObject(payload));
            if (response.IsSuccessStatusCode)
            {
                var adlaAccount = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                return new ActionResponse(ActionStatus.Success, adlaAccount, true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new ActionResponse(ActionStatus.Failure, error, null, DefaultErrorCodes.DefaultErrorCode, "CreateADLAInstance");
        }

        private async Task<bool> CreateAdlsAsync(string accountName, string location, string token, string resourceGroup, string subscriptionId)
        {
            List<string> validAdlsLocations = new List<string>(new string[] { "eastus2", "northeurope", "centralus", "westeurope" });
            if (!validAdlsLocations.Contains(location))
            {
                return false;
            }

            AzureHttpClient client = new AzureHttpClient(token, subscriptionId, resourceGroup);

            dynamic payload = new ExpandoObject();
            payload.location = location;

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Put,
               $"providers/Microsoft.DataLakeStore/accounts/{accountName}", "2016-11-01", JsonUtility.GetJsonStringFromObject(payload));
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }
    }
}