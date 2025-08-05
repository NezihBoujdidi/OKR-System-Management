import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubscriptionComponent } from './components/subscription.component';
import { UpgradePlansComponent } from './components/upgrade-plans.component';
import { UpgradeButtonComponent } from './components/upgrade-button.component';
import { BillingHistoryComponent } from './components/billing-history.component';
import { SharedModule } from '../../shared/shared.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SubscriptionRoutingModule } from './subscription-routing.module';

@NgModule({
  declarations: [
    UpgradeButtonComponent,
    BillingHistoryComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    FormsModule,
    ReactiveFormsModule,
    SubscriptionRoutingModule
  ],
  exports: [
    UpgradeButtonComponent,
    BillingHistoryComponent
  ]
})
export class SubscriptionModule { } 