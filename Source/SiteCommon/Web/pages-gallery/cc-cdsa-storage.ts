import { ViewModelBase } from '../services/view-model-base';

export class CDSAStorage extends ViewModelBase {
    storageAccountName: string = this.MS.DataStore.getValue('StorageAccountName');
    storageAccountKey: string = this.MS.DataStore.getValue('StorageAccountKey');
    keyVaultSubscriptionId: string = this.MS.DataStore.getValue('KeyVaultSubscriptionId');
    keyVaultResourceGroupName: string = this.MS.DataStore.getValue('KeyVaultResourceGroupName');
    keyVaultName: string = this.MS.DataStore.getValue('KeyVaultName');
    keyVaultSecretPath: string = this.MS.DataStore.getValue('KeyVaultSecretPath');

    async onLoaded(): Promise<void> {
        super.onLoaded();

        if (this.storageAccountName && this.storageAccountKey) {
            this.isValidated = true;
        } else {
            this.isValidated = false;
        }
    }

    async onChange(): Promise<any> {
        if (this.storageAccountName && this.storageAccountKey) {
            this.isValidated = true;
        } else {
            this.isValidated = false;
        }
    }
}