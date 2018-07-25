using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.WindowsAzure.Storage.Auth;
using System.IO;
using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Rest;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using System.Threading;
using System.Net.Http.Headers;
using Microsoft.Azure.Management.DataLake.Analytics;
using Microsoft.Azure.Management.DataLake.Analytics.Models;

namespace Microsoft.Deployment.Actions.AzureCustom.AppInsights
{
    [Export(typeof(IAction))]
    public class UploadToADLS : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            string azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            string subscriptionId = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            string resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");

            string tenant = request.DataStore.GetValue("SPNTenantId");
            string appId = request.DataStore.GetValue("SPNAppId");
            string appKey = request.DataStore.GetValue("SPNKey");

            string ADLAName = request.DataStore.GetValue("ADLAName");
            string ADLSName = request.DataStore.GetValue("ADLSName");

            var domain = tenant;
            ClientCredential cred = new ClientCredential(appId, appKey);
            var token = await ApplicationTokenProvider.LoginSilentAsync(domain, cred);
            // - use user token for testing only - var token = new ServiceClientCredImp(azureToken);
            var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(token);
            
            var newtonsoft = Path.Combine(request.Info.App.AppFilePath, "Service/libraries/Newtonsoft.Json.dll");
            var customLib = Path.Combine(request.Info.App.AppFilePath, "Service/libraries/USQLCSharpProject1.dll");
            var script = Path.Combine(request.Info.App.AppFilePath, "Service/libraries/Script.usql");
            
            adlsFileSystemClient.FileSystem.UploadFile(ADLSName, newtonsoft, "/library/Newtonsoft.Json.dll",-1,false,true);
            adlsFileSystemClient.FileSystem.UploadFile(ADLSName, customLib, "/library/USQLCSharpProject1.dll", -1, false, true);
            adlsFileSystemClient.FileSystem.UploadFile(ADLSName, script, "/scripts/script.usql", -1, false, true);

            return new ActionResponse(ActionStatus.Success);
        }
    }

    public class ServiceClientCredImp : ServiceClientCredentials
    {
        public ServiceClientCredImp(string token)
        {
            this.Token = token;
        }

        public string Token { get; }

        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.Token);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}