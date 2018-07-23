import { ViewModelBase } from '../services/view-model-base';
import { RouteConfig } from 'aurelia-router';

export class AxDatasource extends ViewModelBase {
    router: RouteConfig[] = [];

    async onLoaded(): Promise<void> {
        super.onLoaded();
        
        this.router = [{
            href: window.location.href.replace(window.location.hash, '#/ConnecttoDynamics365'),
            name: 'Dynamics365OnCloud',
            nav: true,
            route: 'Dynamics365OnCloud',
            //title: 'Dynamics 365 Finance & Operations On Cloud'
            title: 'Dynamics 365 F&O On Cloud'
        },
        {
            name: 'Dynamics365OnPremise',
            nav: false,
            route: 'Dynamics365OnPremise',
            //title: 'Dynamics 365 Finance & Operations On Premise'
            title: 'Dynamics 365 F&O On Premise'
        },
        {
            name: 'DynamicsAX2012',
            nav: false,
            route: 'DynamicsAX2012',
            title: 'Dynamics AX2012 R3'
        }];
    }
}