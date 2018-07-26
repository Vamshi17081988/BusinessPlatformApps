using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;


namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class CreateCDSAKeyVault : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var rawAzureToken = request.DataStore.GetJson("AzureToken");
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string ADLAName = request.DataStore.GetValue("ADLAName");
            string ADLSName = request.DataStore.GetValue("ADLSName");
            string vaultName = request.DataStore.GetValue("KeyVaultName"); 

            string StorageAccountName = request.DataStore.GetValue("StorageAccountName");
            string StorageAccountKey= request.DataStore.GetValue("StorageAccountKey");

            string tenant = request.DataStore.GetValue("SPNTenantId");
            string appId = request.DataStore.GetValue("SPNAppId");
            string objectId = request.DataStore.GetValue("SPNObjectId");
            string appKey = request.DataStore.GetValue("SPNKey");
            

            var userObjectId = AzureUtility.GetOIDFromToken(rawAzureToken);

            var token = new ServiceClientCredImp(azureToken);

            Microsoft.Azure.Management.KeyVault.Fluent.KeyVaultManagementClient client = 
                new Azure.Management.KeyVault.Fluent.KeyVaultManagementClient(token);
            client.SubscriptionId = subscriptionId;

            var accountParams = new Azure.Management.KeyVault.Fluent.Models.VaultCreateOrUpdateParametersInner();
            accountParams.Location = "centralus";
            accountParams.Properties.TenantId = Guid.Parse(tenant);
            accountParams.Properties.AccessPolicies = new List<AccessPolicyEntry>();

            AccessPolicyEntry entry = new AccessPolicyEntry();
            entry.ObjectId = "8c489c13-bc34-4ab1-a10f-8298d2da7f27";
            entry.TenantId = Guid.Parse("72f988bf-86f1-41af-91ab-2d7cd011db47");
            entry.Permissions = new Permissions();
            entry.Permissions.Keys = new List<string>() { "all" };
            entry.Permissions.Secrets = new List<string>() { "all" };

            AccessPolicyEntry entry2 = new AccessPolicyEntry();
            entry2.ObjectId = userObjectId;
            entry2.TenantId = Guid.Parse(tenant);
            entry2.Permissions = new Permissions();
            entry2.Permissions.Keys = new List<string>() { "all" };
            entry2.Permissions.Secrets = new List<string>() { "all" };

            AccessPolicyEntry entry3 = new AccessPolicyEntry();
            entry3.ObjectId = objectId;
            entry3.TenantId = Guid.Parse(tenant);
            entry3.Permissions = new Permissions();
            entry3.Permissions.Keys = new List<string>() { "all" };
            entry3.Permissions.Secrets = new List<string>() { "all" };

            accountParams.Properties.AccessPolicies.Add(entry);
            accountParams.Properties.AccessPolicies.Add(entry2);
            accountParams.Properties.AccessPolicies.Add(entry3);
            var keyVaultCreation = await client.Vaults.CreateOrUpdateWithHttpMessagesAsync(resourceGroup, vaultName, accountParams);
            if(keyVaultCreation.Response.IsSuccessStatusCode)
            {
                request.DataStore.AddToDataStore("KeyVaultName", vaultName);
                return new ActionResponse(ActionStatus.Success);
            }

            return new ActionResponse(ActionStatus.Failure);
        }
    }
}