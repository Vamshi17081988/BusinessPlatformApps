import { ActionResponse } from "../../../../../SiteCommon/Web/models/action-response";

import { DataStoreType } from '../../../../../SiteCommon/Web/enums/data-store-type';
import { ViewModelBase } from '../../../../../SiteCommon/Web/services/view-model-base';

export class CunaLogin extends ViewModelBase {

    public isValidated: boolean = false;

    public async onClickLogin(): Promise<void> {
        await this.MS.UtilityService.connectToCuna();
    }

    async onLoaded(): Promise<void> {
        super.onLoaded();

        if (await this.validateCunaToken()) {
            this.setValidated();
        }
    }

    async onNavigatingNext(): Promise<boolean> {
        return await this.validateCunaToken();
    }

    async validateCunaToken(): Promise<boolean> {
        let token = await this.getCunaToken();
        if (token && token !== '') {
            this.MS.DataStore.addToDataStore('CunaSamlAuthToken', token, DataStoreType.Private);
            let isValidToken: ActionResponse = await this.MS.HttpService.executeAsync('Microsoft-ValidateCunaToken');
            if (isValidToken.IsSuccess) {
                this.MS.UtilityService.removeItem('queryUrl');
                return true;
            }
        }
        return false;
    }

    async getCunaToken(): Promise<string> {
        let cookieName = 'samlAuthCode';
        const nameLenPlus = (cookieName.length + 1);
        let cookieValue = document.cookie
            .split(';')
            .map(c => c.trim())
            .filter(cookie => {
                return cookie.substring(0, nameLenPlus) === `${cookieName}=`;
            })
            .map(cookie => {
                return decodeURIComponent(cookie.substring(nameLenPlus));
            })[0] || null;

        return cookieValue;
    }
}
