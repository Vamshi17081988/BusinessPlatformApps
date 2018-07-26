using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.WindowsAzure.Storage.Auth;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class CreateStorageAccount : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var rawAzureToken = request.DataStore.GetJson("AzureToken");
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string storageAccountName = request.DataStore.GetValue("StorageAccountName");
            string container = request.DataStore.GetValue("StorageAccountContainer");

            var token = new ServiceClientCredImp(azureToken);
            Microsoft.Azure.Management.Storage.Fluent.StorageManagementClient client = 
                new Azure.Management.Storage.Fluent.StorageManagementClient(token);
            client.SubscriptionId = subscriptionId;
            var param = new Azure.Management.Storage.Fluent.Models.StorageAccountCreateParametersInner();
            param.Location = "centralus";
            param.Sku = new Azure.Management.Storage.Fluent.Models.SkuInner();
            param.Sku.Name = Azure.Management.Storage.Fluent.Models.SkuName.StandardLRS;

            var response = await client.StorageAccounts.CreateWithHttpMessagesAsync(resourceGroup, storageAccountName, param);

            if(response.Response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Success);
            }
            return new ActionResponse(ActionStatus.Failure);
        }
    }
}