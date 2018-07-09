using System;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;

using Microsoft.Deployment.Common.ErrorCode;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.WindowsAzure.Storage;

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class AddContinuousExport : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            var AIInstance = request.DataStore.GetJson("SelectedAppInsightsInstance");
            string AIInstanceName = AIInstance["name"].ToString();
            string AILocation = AIInstance["location"].ToString();
            string AIResourceGroup = ParseResourceGroupFromId(AIInstance["id"].ToString());
            if (string.IsNullOrEmpty(AIResourceGroup))
            {
                return new ActionResponse(ActionStatus.Failure, AIInstance, null, DefaultErrorCodes.DefaultErrorCode,
                   "BadResourceGroupInAppInsightsId");
            }

            string storageAccountArmTemplatePath = Path.Combine(request.ControllerModel.SiteCommonFilePath, "Service/Arm/storageAccount.json");
            string dataExportStorageAccountName = "aidataexport" + Path.GetRandomFileName().Replace(".", "").Substring(0, 8).ToLower();
            string dataExportContainerName = "aitelemetry";

            var dataExportStorageAccount = await CreateStorageAccountAsync(request, dataExportStorageAccountName, AILocation, storageAccountArmTemplatePath);
            if(!dataExportStorageAccount)
            {
                return new ActionResponse(ActionStatus.Failure, null, null, DefaultErrorCodes.DefaultErrorCode, "UnableToCreateStorageAccount");
            }

            string sasKey = await GetSasKey(azureToken, subscriptionId, resourceGroup, dataExportStorageAccountName, dataExportContainerName);
            if(string.IsNullOrEmpty(sasKey))
            {
                return new ActionResponse(ActionStatus.Failure, null, null, DefaultErrorCodes.DefaultErrorCode, "UnableToGetSasKey");
            }

            dynamic payload = new ExpandoObject();
            payload.RecordTypes = "Requests,Exceptions,Event,Messages,Metrics,PageViewPerformance,PageViews,Rdd,Availability,PerformanceCounters";
            payload.DestinationType = "Blob";
            payload.IsEnabled = true;
            payload.DestinationStorageLocationId = AILocation;
            payload.DestinationStorageSubscriptionId = subscriptionId;
            payload.DestinationAddress = $"https://{dataExportStorageAccountName}.blob.core.windows.net/{dataExportContainerName}{sasKey}";
            payload.DestinationAccountId = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Storage/storageAccounts/{dataExportStorageAccountName}";

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, AIResourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post,
                $"providers/Microsoft.Insights/components/{AIInstanceName}/exportconfiguration", "2015-05-01", JsonUtility.GetJsonStringFromObject(payload));
            if (response.IsSuccessStatusCode)
            {
                var continuousExport = JArray.Parse(await response.Content.ReadAsStringAsync());
                return new ActionResponse(ActionStatus.Success, continuousExport, true);
            }

            var error = await response.Content.ReadAsStringAsync();
            return new ActionResponse(ActionStatus.Failure, error, null, DefaultErrorCodes.DefaultErrorCode, "AddContinuousExport");
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

        private async Task<bool> CreateStorageAccountAsync(ActionRequest request, string accountName, string location, string storageAccountArmTemplatePath)
        {
            request.DataStore.AddToDataStore("SelectedLocation", "Name", location);
            request.DataStore.AddToDataStore("StorageAccountName", accountName);
            request.DataStore.AddToDataStore("DeploymentName", "StorageAccount");
            request.DataStore.AddToDataStore("StorageAccountType", "Standard_LRS");
            request.DataStore.AddToDataStore("StorageAccountEncryptionEnabled", "true");

            ActionResponse createStorageAccountResponse = await RequestUtility.CallAction(request, "Microsoft-CreateAzureStorageAccount");

            if (!createStorageAccountResponse.IsSuccess)
            {
                return false;
            }

            ActionResponse armDeploymentResponse = await RequestUtility.CallAction(request, "Microsoft-WaitForArmDeploymentStatus");

            if (!createStorageAccountResponse.IsSuccess)
            {
                return false;
            }

            return true;
        }

        private async Task<string> GetSasKey(string token, string subscriptionId, string resourceGroup, string storageAccountName, string container)
        {
            AzureHttpClient client = new AzureHttpClient(token, subscriptionId, resourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post, $"providers/Microsoft.Storage/storageAccounts/{storageAccountName}/listKeys", "2016-01-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var subscriptionKeys = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                string key = subscriptionKeys["keys"][0]["value"].ToString();
                string connectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={key};EndpointSuffix=core.windows.net";

                var storageAccount = CloudStorageAccount.Parse(connectionString);
                var storageBlobClient = storageAccount.CreateCloudBlobClient();
                var containerRef = storageBlobClient.GetContainerReference(container);
                await containerRef.CreateIfNotExistsAsync();

                SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy();
                sasConstraints.SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddYears(5);
                sasConstraints.Permissions = SharedAccessBlobPermissions.List | 
                SharedAccessBlobPermissions.Write  | 
                SharedAccessBlobPermissions.Read  | 
                SharedAccessBlobPermissions.Create |
                SharedAccessBlobPermissions.Add |
                SharedAccessBlobPermissions.Delete;

                return containerRef.GetSharedAccessSignature(sasConstraints);                                
            }

            return null;
        }
    }
}