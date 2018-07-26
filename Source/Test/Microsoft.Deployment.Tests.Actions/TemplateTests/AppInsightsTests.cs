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

            var response = TestManager.ExecuteAction("Microsoft-GrantSPNPermissionsToResourceGroup", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task DeployADLAViaSPN()
        {
            var dataStore = await TestManager.GetDataStore();
            ActionResponse response = null;

            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo4");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo4");

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
            paramsArm.Add("factoryName", "testmofactory234");
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

            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo4");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo4");

            dataStore.AddToDataStore("SPNAppId", "5d7ddf88-b706-441c-b13b-a75b06715d43");
            dataStore.AddToDataStore("SPNKey", "6EcZhy+jrGHcB0S2mZSD55MIJg1BZ+q7rr5p38GlCKz3cZwTcwElIGWWHbc=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "5d82f050-efe9-4f2d-b2bf-6a3f17534844");
      
            dataStore.AddToDataStore("StorageAccountName", "testmostoragerequired2");
            dataStore.AddToDataStore("StorageAccountContainer", "appinsightsouput");

            dataStore.AddToDataStore("KeyVaultName", "pbikeyvaultunqrz2");
            dataStore.AddToDataStore("KeyVaultSecretName", "pbisecret");


            //response = TestManager.ExecuteAction("Microsoft-CreateStorageAccount", dataStore, "Microsoft-ApplicationInsightsTemplate");
            //Assert.IsTrue(response.IsSuccess);

            this.UIStepsPowerBI(dataStore);
           
            dataStore.AddToDataStore("PBIWorkspaceId", "19977aed-0755-40b9-aab4-a33f2f4e022c");

            // Create KeyVault and give PBI Access
            response = TestManager.ExecuteAction("Microsoft-CreateCDSAKeyVault", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
            
            response = TestManager.ExecuteAction("Microsoft-CreateEntryInKeyVault", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            // Upload datapool to destination
            dataStore.AddToDataStore("filepath", "Service/PowerBI/model.dplx");
            response = TestManager.ExecuteAction("Microsoft-UploadDPLXToBlob", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            // mount datapool inside powerbi
            dataStore.AddToDataStore("DatapoolName", "testdplx");
            dataStore.AddToDataStore("DatapoolDescription", "test description");
            dataStore.AddToDataStore("KeyVaultSubscriptionId", dataStore.GetJson("SelectedSubscription", "SubscriptionId"));
            dataStore.AddToDataStore("KeyVaultResourceGroupName", dataStore.GetValue("SelectedResourceGroup"));
            dataStore.AddToDataStore("KeyVaultSecretPath", dataStore.GetValue("KeyVaultSecretName"));

            response = TestManager.ExecuteAction("Microsoft-CreatePBIDatapoolReference", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            // Upload PBIX
            dataStore.AddToDataStore("PBIXLocation", "Service/PowerBI/appinsights.pbix");
            response = TestManager.ExecuteAction("Microsoft-PublishPBIReportCDSA", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);

            // Change parameters for PBIX
            response = TestManager.ExecuteAction("Microsoft-UpdatePBIParameters", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
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

        public void UIStepsPowerBI(DataStore dataStore)
        {
            dataStore.AddToDataStore("oauthType", "powerbi");
            // Login to PBIX
            var powerBI = AAD.GetUserTokenFromPopup("powerbi").Result;
            dataStore.AddToDataStore("PBIToken", powerBI.GetJson("PBIToken").GetJObject(), DataStoreType.Private);

            var response = TestManager.ExecuteAction("Microsoft-GetPBIClusterUri", dataStore, "Microsoft-ApplicationInsightsTemplate");
            response = TestManager.ExecuteAction("Microsoft-GetPBIWorkspacesCDSA", dataStore, "Microsoft-ApplicationInsightsTemplate");

            // Select a workspace
            var workspaceId = JArray.Parse(response.Body.ToString())[22]["id"];
            dataStore.AddToDataStore("PBIWorkspaceId", workspaceId);
        }

    }
}

