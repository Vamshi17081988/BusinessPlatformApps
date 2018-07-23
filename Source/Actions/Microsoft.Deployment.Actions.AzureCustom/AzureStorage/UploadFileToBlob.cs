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
    public class UploadFileToBlob : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string storageAccountName = request.DataStore.GetValue("StorageAccountName");
            string storageAccountKey = request.DataStore.GetValue("StorageAccountKey");
            string container = request.DataStore.GetValue("StorageAccountContainer");
            string blobName = request.DataStore.GetValue("DestinationFileName");
            string filepath = request.DataStore.GetValue("filepath");

            StorageCredentials creds = new StorageCredentials(storageAccountName, storageAccountKey);
            WindowsAzure.Storage.CloudStorageAccount storageAccountClient = new WindowsAzure.Storage.CloudStorageAccount(creds, true);
            var blobClient = storageAccountClient.CreateCloudBlobClient();
            var containerRef = blobClient.GetContainerReference(container);
            await containerRef.CreateIfNotExistsAsync();
            var blobRef = containerRef.GetBlockBlobReference(blobName);

            var completeFilePath = Path.Combine(request.Info.App.AppFilePath, filepath);
            await blobRef.UploadFromFileAsync(completeFilePath);
            return new ActionResponse(ActionStatus.Success);

        }
    }
}