using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;

namespace Microsoft.Deployment.Actions.AzureCustom.Common
{
    [Export(typeof(IAction))]
    public class GetStorageAccountKey : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            var subscription = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            var resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");
            var location = request.DataStore.GetJson("SelectedLocation", "Name");
            var storageAccountName = request.DataStore.GetValue("StorageAccountName");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscription, resourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post, $"providers/Microsoft.Storage/storageAccounts/{storageAccountName}/listKeys", "2016-01-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var subscriptionKeys = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                string key = subscriptionKeys["keys"][0]["value"].ToString();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={key};EndpointSuffix=core.windows.net";
                JObject newStorageAccountKey = new JObject();
                newStorageAccountKey.Add("StorageAccountKey", key);
                newStorageAccountKey.Add("StorageAccountConnectionString", connectionString);
                request.DataStore.AddToDataStore("StorageAccountKey", key);
                request.DataStore.AddToDataStore("StorageAccountConnectionString", connectionString);
                return new ActionResponse(ActionStatus.Success, newStorageAccountKey, true);
            }

            return new ActionResponse(ActionStatus.Failure);
        }
    }

    [Export(typeof(IAction))]
    public class GetStorageAccountKeyGeneric : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            var subscription = request.DataStore.GetValue("SubscriptionId");
            var resourceGroup = request.DataStore.GetValue("ResourceGroup");
            var storageAccountName = request.DataStore.GetValue("StorageAccountName");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscription, resourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post, $"providers/Microsoft.Storage/storageAccounts/{storageAccountName}/listKeys", "2016-01-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var subscriptionKeys = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                string key = subscriptionKeys["keys"][0]["value"].ToString();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={key};EndpointSuffix=core.windows.net";
                JObject newStorageAccountKey = new JObject();
                newStorageAccountKey.Add("StorageAccountKey", key);
                newStorageAccountKey.Add("StorageAccountConnectionString", connectionString);
                request.DataStore.AddToDataStore("StorageAccountKey", key);
                request.DataStore.AddToDataStore("StorageAccountConnectionString", connectionString);
                return new ActionResponse(ActionStatus.Success, newStorageAccountKey, true);
            }

            return new ActionResponse(ActionStatus.Failure);
        }
    }
}