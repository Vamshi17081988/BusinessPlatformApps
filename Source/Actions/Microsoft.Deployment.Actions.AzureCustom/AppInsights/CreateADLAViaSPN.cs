using System;
using System.Collections.Generic;
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
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage;

using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class AddADLAViaSPN : BaseAction
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

            string tenant = request.DataStore.GetValue("SPNTenantId");
            string appId = request.DataStore.GetValue("SPNAppId");
            string appKey = request.DataStore.GetValue("SPNKey");

            string aiStorageName = request.DataStore.GetJson("SelectedAppInsightsExport", "StorageName");
            string aiSubscriptionId = request.DataStore.GetJson("SelectedAppInsightsExport", "DestinationStorageSubscriptionId");
            string aiResourceGroup = request.DataStore.GetJson("SelectedAppInsightsExport", "DestinationAccountId").Split('/')[4];

            ClientCredential cred = new ClientCredential(appId, appKey);
            var token = await ApplicationTokenProvider.LoginSilentAsync(tenant, cred);
            
            Microsoft.Azure.Management.DataLake.Store.DataLakeStoreAccountManagementClient client = 
                new Azure.Management.DataLake.Store.DataLakeStoreAccountManagementClient(token);
            client.SubscriptionId = subscriptionId;

            var accountParam = new Azure.Management.DataLake.Store.Models.DataLakeStoreAccount();
            //accountParam.DefaultGroup = appId;
            accountParam.Location = "centralus";
            var createADLS = await client.Account.CreateWithHttpMessagesAsync(resourceGroup, ADLSName, accountParam);
            if(!createADLS.Response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Failure);
            }

            Microsoft.Azure.Management.DataLake.Analytics.DataLakeAnalyticsAccountManagementClient client2 =
                new Azure.Management.DataLake.Analytics.DataLakeAnalyticsAccountManagementClient(token);
            client2.SubscriptionId = subscriptionId;
            var analyticsParam = new Azure.Management.DataLake.Analytics.Models.DataLakeAnalyticsAccount();
            var dataSourceDataLake = new Azure.Management.DataLake.Analytics.Models.DataLakeStoreAccountInfo();
            dataSourceDataLake.Name = ADLSName;
            analyticsParam.DataLakeStoreAccounts = new List<Azure.Management.DataLake.Analytics.Models.DataLakeStoreAccountInfo>();
            analyticsParam.DataLakeStoreAccounts.Add(dataSourceDataLake);
            analyticsParam.Location = "centralus";
            analyticsParam.DefaultDataLakeStoreAccount = ADLSName;

            var storageInfo = new Azure.Management.DataLake.Analytics.Models.StorageAccountInfo();
            storageInfo.Name = aiStorageName;
            storageInfo.AccessKey = await GetStorageKey(azureToken, aiSubscriptionId, aiResourceGroup, aiStorageName);
            analyticsParam.StorageAccounts = new List<Azure.Management.DataLake.Analytics.Models.StorageAccountInfo>();
            analyticsParam.StorageAccounts.Add(storageInfo);
            
            var createADLA = await client2.Account.CreateWithHttpMessagesAsync(resourceGroup, ADLAName, analyticsParam);
            if(!createADLA.Response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Failure);
            }

            return new ActionResponse(ActionStatus.Success);
        }

        public async Task<string> GetStorageKey(string azureToken, string subscriptionId, string resourceGroup, string accountName)
        {
            AzureHttpClient client = new AzureHttpClient(azureToken, subscriptionId, resourceGroup);

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post, $"providers/Microsoft.Storage/storageAccounts/{accountName}/listKeys", "2016-01-01", string.Empty);
            if (response.IsSuccessStatusCode)
            {
                var subscriptionKeys = JsonUtility.GetJObjectFromJsonString(await response.Content.ReadAsStringAsync());
                string key = subscriptionKeys["keys"][0]["value"].ToString();
                return key;
            }

            return string.Empty;
        }
    }
}