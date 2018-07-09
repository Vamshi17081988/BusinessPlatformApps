import { DataStoreType } from '../enums/data-store-type';

import { ViewModelBase } from "../services/view-model-base";
import { ActionResponse } from "../models/action-response";

export class AppInsightsConfig extends ViewModelBase {
    selectedSubscriptionName: string = '';
    appInsightsList: any[] = [];
    selectedAppInsightsInstance: string = '';

    async getAppInsightsInstances(): Promise<void> {
        let appInsightsInstances: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetAppInsightsInstances');
        if (appInsightsInstances.IsSuccess) {
            this.appInsightsList = appInsightsInstances.Body.value;

            if (!this.appInsightsList || (this.appInsightsList && this.appInsightsList.length === 0)) {
                this.MS.ErrorService.message = this.MS.Translate.APP_INSIGHTS_ERROR + this.selectedSubscriptionName;
                this.isValidated = false;
            } else {
                this.selectedAppInsightsInstance = this.appInsightsList[0].name;
                this.isValidated = true;
                console.log(this.selectedAppInsightsInstance);
            }
        }
    }

    async changeAppInsightsInstance(): Promise<void> {
        if (this.selectedAppInsightsInstance) {
            this.isValidated = true;
            console.log(this.selectedAppInsightsInstance);
        }
        else {
            this.isValidated = false;
        }
    }

    async onLoaded(): Promise<void> {
        super.onLoaded();
        
        if (!this.selectedSubscriptionName) {
            var subscriptionObject = this.MS.DataStore.getJson('SelectedSubscription');
            this.selectedSubscriptionName = subscriptionObject.DisplayName;
        }

        this.getAppInsightsInstances();
    }

    async onNavigatingNext(): Promise<boolean> {
        let appInsightsObject = this.appInsightsList.find(x => x.name === this.selectedAppInsightsInstance)
        this.MS.DataStore.addToDataStore('SelectedAppInsightsInstance', appInsightsObject, DataStoreType.Public);

        return true;
    }
}