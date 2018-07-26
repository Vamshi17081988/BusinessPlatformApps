using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Deployment.Actions.AzureCustom.AzureToken;
using Microsoft.Deployment.Common;
using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class CreateEntryInKeyVault : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var rawAzureToken = request.DataStore.GetJson("AzureToken");
            string refreshToken = request.DataStore.GetJson("AzureToken", "refresh_token");
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string keyvaultName = request.DataStore.GetValue("KeyVaultName");
            string storageAccountName = request.DataStore.GetValue("StorageAccountName");
            string container = request.DataStore.GetValue("StorageAccountContainer");

            string tenant = request.DataStore.GetValue("SPNTenantId");
            string appId = request.DataStore.GetValue("SPNAppId");
            string objectId = request.DataStore.GetValue("SPNObjectId");
            string appKey = request.DataStore.GetValue("SPNKey");

            string workspaceId = request.DataStore.GetValue("PBIWorkspaceId");

            string vaultUrl = $"https://{keyvaultName}.vault.azure.net/";
            string secretName = request.DataStore.GetValue("KeyVaultSecretName");

            /// Create CDS-A payload
            CDSASecretModel model = new CDSASecretModel()
            {
                storageDetails = new Storagedetails()
                {
                    accountKey = await GetStorageKey(azureToken, subscriptionId, resourceGroup, storageAccountName),
                    accountName = storageAccountName,
                    containerName = container
                },
                allowedPrincipals = new Allowedprincipal[1]
            };

            model.allowedPrincipals[0] = new Allowedprincipal();
            model.allowedPrincipals[0].type = "upn";
            model.allowedPrincipals[0].tenantId = tenant;
            model.allowedPrincipals[0].upn = AzureUtility.GetEmailFromToken(rawAzureToken);

            //// Create Entry in keyvault
            ClientCredential cred = new ClientCredential(appId, appKey);
            var token = await ApplicationTokenProvider.LoginSilentAsync(tenant, cred);

            var keyVaultToken = await GetKeyVaultTokenAsync(tenant, appId, appKey);
            KeyVaultClient kvClient = new KeyVaultClient(new TokenCredentials(keyVaultToken));
            var result = await kvClient.SetSecretWithHttpMessagesAsync(vaultUrl,
                                                               secretName,
                                                               JsonUtility.Serialize<CDSASecretModel>(model));
            if (result.Response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Success);
            }

            return new ActionResponse(ActionStatus.Failure);
        }

        public async Task<string> GetKeyVaultTokenAsync(string tenantId, string clientId, string clientSecret)
        {
            JObject tokenObj;
            using (HttpClient httpClient = new HttpClient())
            {
                string tokenUrl = string.Format(Constants.AzureTokenUri, tenantId);
                Dictionary<string, string> message = new Dictionary<string, string>
                {
                    {"client_id", clientId},
                    {"client_secret", Uri.EscapeDataString(clientSecret)},
                    {"resource", Uri.EscapeDataString(Constants.AzureKeyVaultApi)},
                    {"grant_type", "client_credentials"}
                };

                StringBuilder builder = new StringBuilder();
                foreach (KeyValuePair<string, string> keyValuePair in message)
                {
                    builder.Append(keyValuePair.Key + "=" + keyValuePair.Value);
                    builder.Append("&");
                }
            
                StringContent content = new StringContent(builder.ToString());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                string response2 = httpClient.PostAsync(new Uri(tokenUrl), content).Result.Content.AsString();
                tokenObj = JsonUtility.GetJsonObjectFromJsonString(response2);
            }

            return tokenObj["access_token"].ToString();
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


    public class CDSASecretModel
    {
        public Storagedetails storageDetails { get; set; }
        public Allowedprincipal[] allowedPrincipals { get; set; }
    }

    public class Storagedetails
    {
        public string containerName { get; set; }
        public string accountName { get; set; }
        public string accountKey { get; set; }
    }

    public class Allowedprincipal
    {
        [JsonProperty(PropertyName = "$type")]
        public string type { get; set; }
        public string upn { get; set; }
        public string tenantId { get; set; }
    }

}