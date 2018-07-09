import { DataStoreType } from "../enums/data-store-type";

import { ViewModelBase } from "../services/view-model-base";
import { ActionResponse } from "../models/action-response";

export class AppInsightsConfig extends ViewModelBase {
    continuousExportsList: any[] = [];
    selectedContinuousExport: string = '';
    addingContinuousExport: boolean = false;

    async getContinuousExports(): Promise<void> {
        let continuousExports: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-GetContinuousExports');
        if (continuousExports.IsSuccess) {
            this.continuousExportsList = continuousExports.Body;
            this.isValidated = true;
        }
    }

    async changeContinuousExport(): Promise<void> {
        if (this.selectedContinuousExport) {
            this.isValidated = true;
        }
        else {
            this.isValidated = false;
        }
    }

    async onLoaded(): Promise<void> {
        super.onLoaded();

        this.selectedContinuousExport = '';
        this.getContinuousExports();
    }

    async onNavigatingNext(): Promise<boolean> {
        let continuousExportObject = this.continuousExportsList.find(x => x.StorageName === this.selectedContinuousExport);

        if (!this.selectedContinuousExport && this.continuousExportsList && this.continuousExportsList.length == 0) {
            this.addingContinuousExport = true;
            let newContinuousExport: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-AddContinuousExport');
            if (newContinuousExport.IsSuccess) {
                continuousExportObject = newContinuousExport.Body[0];
                this.addingContinuousExport = false;
            }
            else {
                // Error cases?
            }
        }

        this.MS.DataStore.addToDataStore('SelectedContinuousExport', continuousExportObject, DataStoreType.Public);

        return true;
    }

}