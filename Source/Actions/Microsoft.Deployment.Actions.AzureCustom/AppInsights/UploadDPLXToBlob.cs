using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.WindowsAzure.Storage.Auth;
using System.IO;

namespace Microsoft.Deployment.Actions.AzureCustom.Common
{
    [Export(typeof(IAction))]
    public class UploadDPLXToBlob : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string storageAccountName = request.DataStore.GetValue("StorageAccountName");
            string container = request.DataStore.GetValue("StorageAccountContainer");
            string filepath = request.DataStore.GetValue("filepath");
            string accountKey = await AzureUtility.GetStorageKey(azureToken, subscriptionId, resourceGroup, storageAccountName);

            StorageCredentials creds = new StorageCredentials(storageAccountName, accountKey);
            WindowsAzure.Storage.CloudStorageAccount storageAccountClient = new WindowsAzure.Storage.CloudStorageAccount(creds, true);
            var blobClient = storageAccountClient.CreateCloudBlobClient();
            var containerRef = blobClient.GetContainerReference(container);
            await containerRef.CreateIfNotExistsAsync();
            var blobRef = containerRef.GetBlockBlobReference("model.dplx");

            var completeFilePath = Path.Combine(request.Info.App.AppFilePath, filepath);

            var dplxFile = System.IO.File.ReadAllText(completeFilePath);
            string storageNameReplaced = dplxFile.Replace("cdsaintegrationbyos", storageAccountName);
            string containerNameReplaced = storageNameReplaced.Replace("custcollectionsbimeasurements", container);

            await blobRef.UploadTextAsync(containerNameReplaced);
            return new ActionResponse(ActionStatus.Success);

        }
    }
}