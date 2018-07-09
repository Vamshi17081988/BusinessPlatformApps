using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Deployment.Tests.Actions.TestHelpers;

using Microsoft.Deployment.Common.Helpers;

namespace Microsoft.Deployment.Tests.Actions.AzureTests
{
    [TestClass]
    public class AppInsightsInstancesTests
    {
        [TestMethod]
        public async Task GetAppInsightsInstancesAndContinuousExports()
        {
            var dataStore = await TestManager.GetDataStore();

            // GetAppInsightsInstances
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);

            // Mock AppInsights Instance
            var mockAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][0];
            dataStore.AddToDataStore("SelectedAppInsightsInstance", mockAppInsightsInstance);

            // GetContinuousExports
            var getContinuousExportsResponse = TestManager.ExecuteAction("Microsoft-GetContinuousExports", dataStore);
            Assert.IsTrue(getContinuousExportsResponse.IsSuccess);
        }

        [TestMethod]
        public async Task AddContinuousExport()
        {
            var dataStore = await TestManager.GetDataStore(true);

            // GetAppInsightsInstances
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);

            // Mock AppInsights Instance
            var mockAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][2];
            dataStore.AddToDataStore("SelectedAppInsightsInstance", mockAppInsightsInstance);

            // AddContinuousExport
            var addContinuousExportResponse = TestManager.ExecuteAction("Microsoft-AddContinuousExport", dataStore);
            Assert.IsTrue(addContinuousExportResponse.IsSuccess);
        }

        [TestMethod]
        public async Task CreateADLAInstance()
        {
            var dataStore = await TestManager.GetDataStore(true);

            // GetAppInsightsInstances
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);

            // Mock AppInsights Instance (change location to valid ADLS option for success case)
            var mockAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][0];
            mockAppInsightsInstance["location"] = "centralus";
            dataStore.AddToDataStore("SelectedAppInsightsInstance", mockAppInsightsInstance);

            // CreateADLAInstance
            var createADLAInstanceResponse = TestManager.ExecuteAction("Microsoft-CreateADLAInstance", dataStore);
            Assert.IsTrue(createADLAInstanceResponse.IsSuccess);
        }

        // Error Tests
        [TestMethod]
        public async Task ValidateErrorGetAppInsightsInstancesAndContinuousExports()
        {
            var dataStore = await TestManager.GetDataStore();

            // GetContinuousExports
            var getContinuousExportsResponse = TestManager.ExecuteAction("Microsoft-GetContinuousExports", dataStore);
            Assert.IsFalse(getContinuousExportsResponse.IsSuccess);
        }

        [TestMethod]
        public async Task ValidateErrorAddContinuousExport()
        {
            var dataStore = await TestManager.GetDataStore();

            // AddContinuousExport
            var addContinuousExportResponse = TestManager.ExecuteAction("Microsoft-AddContinuousExport", dataStore);
            Assert.IsFalse(addContinuousExportResponse.IsSuccess);
        }

        [TestMethod]
        public async Task ValidateErrorCreateADLAInstance()
        {
            var dataStore = await TestManager.GetDataStore(true);

            // GetAppInsightsInstances
            var getAppInsightsInstancesResponse = TestManager.ExecuteAction("Microsoft-GetAppInsightsInstances", dataStore);
            Assert.IsTrue(getAppInsightsInstancesResponse.IsSuccess);

            // Mock AppInsights Instance (change location to invalid ADLS option for error case)
            var mockAppInsightsInstance = JsonUtility.GetJObjectFromObject(getAppInsightsInstancesResponse.Body)["value"][0];
            mockAppInsightsInstance["location"] = "eastus";
            dataStore.AddToDataStore("SelectedAppInsightsInstance", mockAppInsightsInstance);

            // CreateADLAInstance
            var createADLAInstanceResponse = TestManager.ExecuteAction("Microsoft-CreateADLAInstance", dataStore);
            Assert.IsFalse(createADLAInstanceResponse.IsSuccess);
        }
    }
}
