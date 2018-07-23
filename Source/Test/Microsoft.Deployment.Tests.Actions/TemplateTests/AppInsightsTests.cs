using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Helpers;
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
        public async Task DeployADLAViaARM()
        {
            var dataStore =  await TestManager.GetDataStore();

            ActionResponse response = null;
            dataStore.AddToDataStore("AzureArmFile", "Service/AzureArm/adla.json");

            JObject paramsArm = new JObject();
            paramsArm.Add("name", "adlaappinsigtsmo");
            paramsArm.Add("storageAccountName", "adlsappinsigtsmo");
            dataStore.AddToDataStore("AzureArmParameters", paramsArm);
            response = await TestManager.ExecuteActionAsync("Microsoft-DeployAzureArmTemplate", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task CreateDataSourceInADLA()
        {
            var dataStore = await TestManager.GetDataStore();
            dataStore.AddToDataStore("ADLAName", "adlaappinsigtsmo");
            dataStore.AddToDataStore("ADLSName", "adlsappinsigtsmo");

            // Will be done inside UI
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);
            var selectedAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][0];
            dataStore.AddToDataStore("SelectedAppInsightsInstance", selectedAppInsightsInstance);

            var getContinuousExportsResponse = TestManager.ExecuteAction("Microsoft-GetContinuousExports", dataStore);
            Assert.IsTrue(getContinuousExportsResponse.IsSuccess);
            var selectedAppInsightsExoirt = JArray.Parse(getContinuousExportsResponse.Body.ToString())[0];
            dataStore.AddToDataStore("SelectedAppInsightsExport", selectedAppInsightsInstance);

            //// Continuing deployment
            // Get Storage Account Key
            string storageName = selectedAppInsightsExoirt["StorageName"].ToString();
            string subscriptionId = selectedAppInsightsExoirt["DestinationStorageSubscriptionId"].ToString();
            string resourceGroup = selectedAppInsightsExoirt["DestinationAccountId"].ToString().Split('/')[4];

            dataStore.AddToDataStore("StorageAccountName", storageName);
            dataStore.AddToDataStore("SubscriptionId", subscriptionId);
            dataStore.AddToDataStore("ResourceGroup", resourceGroup);
            var accountKeyResponse = TestManager.ExecuteAction("Microsoft-GetStorageAccountKeyGeneric", dataStore);
            Assert.IsTrue(accountKeyResponse.IsSuccess);

            var adlaDataSourceResponse = TestManager.ExecuteAction("Microsoft-AddADLADataSource", dataStore);
            Assert.IsTrue(adlaDataSourceResponse.IsSuccess);


            // Upload Assemblies to blob store
            dataStore.AddToDataStore("StorageAccountContainer", "library");
            dataStore.AddToDataStore("DestinationFileName", "Newtonsoft.Json.dll");
            dataStore.AddToDataStore("filepath", "Service/libraries/Newtonsoft.Json.dll");
            var uploadFileToBlobResponse = TestManager.ExecuteAction("Microsoft-UploadFileToBlob", dataStore, "Microsoft-ApplicationInsightsTemplate");
            Assert.IsTrue(uploadFileToBlobResponse.IsSuccess);
        }
    }
}

