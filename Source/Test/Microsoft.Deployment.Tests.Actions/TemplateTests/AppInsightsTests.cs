using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.Deployment.Tests.Actions.AzureTests;
using Microsoft.Deployment.Tests.Actions.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Deployment.Tests.Actions.TemplateTests
{
    [TestClass]
    public class AppInsightsTests
    {
        //[TestMethod]
        //public async Task DeployADLAViaARM()
        //{
        //    var dataStore =  await TestManager.GetDataStore();

        //    ActionResponse response = null;
        //    dataStore.AddToDataStore("AzureArmFile", "Service/AzureArm/adla.json");

        //    JObject paramsArm = new JObject();
        //    paramsArm.Add("name", "adlaappinsigtsmo");
        //    paramsArm.Add("storageAccountName", "adlsappinsigtsmo");
        //    dataStore.AddToDataStore("AzureArmParameters", paramsArm);
        //    response = await TestManager.ExecuteActionAsync("Microsoft-DeployAzureArmTemplate", dataStore, "Microsoft-ApplicationInsightsTemplate");
        //    Assert.IsTrue(response.IsSuccess);
        //}

        [TestMethod]
        public async Task CreateSPN()
        {
            var dataStore = await TestManager.GetDataStore();
            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo");
            var permissions = JToken.Parse(HelperStringsJson.BasicPermission);
            dataStore.AddToDataStore("Permissions", permissions);
            var response = TestManager.ExecuteAction("Microsoft-CreateADSPN", dataStore);
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task GiveSPNAccess()
        {
            var dataStore = await TestManager.GetDataStore(true);
            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo");

            dataStore.AddToDataStore("SPNAppId", "5d7ddf88-b706-441c-b13b-a75b06715d43");
            dataStore.AddToDataStore("SPNKey", "6EcZhy+jrGHcB0S2mZSD55MIJg1BZ+q7rr5p38GlCKz3cZwTcwElIGWWHbc=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "5d82f050-efe9-4f2d-b2bf-6a3f17534844");
            //var response = TestManager.ExecuteAction("Microsoft-UploadToADLS", dataStore, "Microsoft-ApplicationInsightsTemplate");

            var response = TestManager.ExecuteAction("Microsoft-GrantSPNPermissionsToResourceGroup", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task DeployADLAViaSPN()
        {
            var dataStore = await TestManager.GetDataStore(true);
            ActionResponse response = null;

            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo3");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo3");

            dataStore.AddToDataStore("SPNAppId", "5d7ddf88-b706-441c-b13b-a75b06715d43");
            dataStore.AddToDataStore("SPNKey", "6EcZhy+jrGHcB0S2mZSD55MIJg1BZ+q7rr5p38GlCKz3cZwTcwElIGWWHbc=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "5d82f050-efe9-4f2d-b2bf-6a3f17534844");
            this.UISteps(dataStore);

            response = await TestManager.ExecuteActionAsync("Microsoft-AddADLAViaSPN", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            response = await TestManager.ExecuteActionAsync("Microsoft-UploadToADLS", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            dataStore.AddToDataStore("AzureArmFile", "Service/AzureArm/adf.json");

            JObject paramsArm = new JObject();
            paramsArm.Add("factoryName", "testmofactory");
            paramsArm.Add("ServicePrincipalKey", dataStore.GetValue("SPNKey"));
            paramsArm.Add("DataLakeAnalyticsName", dataStore.GetValue("ADLAName"));
            paramsArm.Add("ServicePrincipalId", dataStore.GetValue("SPNAppId"));
            paramsArm.Add("Tenant", dataStore.GetValue("SPNTenantId"));
            paramsArm.Add("SubscriptionId", dataStore.GetJson("SelectedSubscription", "SubscriptionId"));
            paramsArm.Add("ResourceGroupName", dataStore.GetValue("SelectedResourceGroup"));
            paramsArm.Add("DataLakeStoreName", dataStore.GetValue("ADLSName"));
      
            dataStore.AddToDataStore("AzureArmParameters", paramsArm);

            response = await TestManager.ExecuteActionAsync("Microsoft-DeployAzureArmTemplate", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task CDSASetUp()
        {
            var dataStore = await TestManager.GetDataStore();
            ActionResponse response = null;

            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo3");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo3");

            dataStore.AddToDataStore("SPNAppId", "5d7ddf88-b706-441c-b13b-a75b06715d43");
            dataStore.AddToDataStore("SPNKey", "6EcZhy+jrGHcB0S2mZSD55MIJg1BZ+q7rr5p38GlCKz3cZwTcwElIGWWHbc=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "5d82f050-efe9-4f2d-b2bf-6a3f17534844");
            dataStore.AddToDataStore("oauthType", "powerbi");

            dataStore.AddToDataStore("StorageAccountName", "testmostoragerequired");
            dataStore.AddToDataStore("StorageAccountContainer", "appinsightsouput");

 
            dataStore.AddToDataStore("KeyVaultName", "pbikeyvaultunqrz");
            dataStore.AddToDataStore("KeyVaultSecretName", "pbisecret");

            // Login to PBIX
            //var powerBI =  AAD.GetUserTokenFromPopup("powerbi").Result;
            //dataStore.AddToDataStore("PBIToken", powerBI.GetJson("PBIToken").GetJObject(), DataStoreType.Private);

            //response = TestManager.ExecuteAction("Microsoft-GetPBIClusterUri", dataStore, "Microsoft-ApplicationInsightsTemplate");
            //response = TestManager.ExecuteAction("Microsoft-GetPBIWorkspacesCDSA", dataStore, "Microsoft-ApplicationInsightsTemplate");

            //// Select a workspace
            //var workspaceId = JArray.Parse(response.Body.ToString())[22]["id"];
            //dataStore.AddToDataStore("PBIWorkspaceId", workspaceId);
            dataStore.AddToDataStore("PBIWorkspaceId", "19977aed-0755-40b9-aab4-a33f2f4e022c");

            // Create KeyVault and give PBI Access
            response = TestManager.ExecuteAction("Microsoft-CreateCDSAKeyVault", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
            
            response = TestManager.ExecuteAction("Microsoft-CreateEntryInKeyVault", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            // Upload datapool to destination

            // mount datapool inside powerbi

            // upload PBIX

            // Change parameters for PBIX

            // Get redirect URI where pbix file is

        }

        public void UISteps(DataStore dataStore)
        {
            // Will be done inside UI
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);
            var selectedAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][0];
            dataStore.AddToDataStore("SelectedAppInsightsInstance", selectedAppInsightsInstance);

            var getContinuousExportsResponse = TestManager.ExecuteAction("Microsoft-GetContinuousExports", dataStore);
            Assert.IsTrue(getContinuousExportsResponse.IsSuccess);
            var selectedAppInsightsExport = JArray.Parse(getContinuousExportsResponse.Body.ToString())[0];
            dataStore.AddToDataStore("SelectedAppInsightsExport", selectedAppInsightsExport);
        }

    }
}

