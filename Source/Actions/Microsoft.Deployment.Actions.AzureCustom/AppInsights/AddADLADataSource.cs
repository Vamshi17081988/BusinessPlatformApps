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
    public class AddADLADataSource : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string ADLAName = request.DataStore.GetValue("ADLAName");
            string ADLSName = request.DataStore.GetValue("ADLSName");
            string StorageAccountName = request.DataStore.GetValue("StorageAccountName");
            string StorageAccountKey= request.DataStore.GetValue("StorageAccountKey");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, resourceGroup);

            JObject paramsArm = new JObject();
            JObject nestedParam = new JObject();
            paramsArm.Add("name", StorageAccountName);
            paramsArm.Add("properties", nestedParam);
            nestedParam.Add("accessKey", StorageAccountKey);
            paramsArm.Add("type", "AzureBlob");
            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Put, $"providers/Microsoft.DataLakeAnalytics/accounts/{ADLAName}/StorageAccounts/{StorageAccountName}", "2016-11-01", paramsArm.ToString());
            if (response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Success);
            }

            return new ActionResponse(ActionStatus.Failure);
        }
    }
}