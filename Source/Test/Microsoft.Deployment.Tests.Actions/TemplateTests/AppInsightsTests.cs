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

            dataStore.AddToDataStore("SPNAppId", "6c08fe5e-8255-4450-8a3c-1a53847a2aee");
            dataStore.AddToDataStore("SPNKey", "n+YK5CgfiEtpEag1wH+LIhB4i6fzpNA3EWNtXzwkpmt/SKcZxP9jzln2nfs=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "ae476a97-afe0-4782-920c-aa51505a24c7");
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

            dataStore.AddToDataStore("SPNAppId", "6c08fe5e-8255-4450-8a3c-1a53847a2aee");
            dataStore.AddToDataStore("SPNKey", "n+YK5CgfiEtpEag1wH+LIhB4i6fzpNA3EWNtXzwkpmt/SKcZxP9jzln2nfs=");
            dataStore.AddToDataStore("SPNTenantId", "72f988bf-86f1-41af-91ab-2d7cd011db47");
            dataStore.AddToDataStore("SPNObjectId", "ae476a97-afe0-4782-920c-aa51505a24c7");
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
        public async Task DeployADLAViaARM()
        {
        //    var dataStore =  await TestManager.GetDataStore();

        //    ActionResponse response = null;
        //    dataStore.AddToDataStore("AzureArmFile", "Service/AzureArm/adla.json");

        //    JObject paramsArm = new JObject();
        //    paramsArm.Add("name", "adlaappinsigtsmo");
        //    paramsArm.Add("storageAccountName", "adlsappinsigtsmo");
        //    dataStore.AddToDataStore("AzureArmParameters", paramsArm);
        //    response = await TestManager.ExecuteActionAsync("Microsoft-DeployAzureArmTemplate", dataStore, "Microsoft-ApplicationInsightsTemplate");
        //    Assert.IsTrue(response.IsSuccess);
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

