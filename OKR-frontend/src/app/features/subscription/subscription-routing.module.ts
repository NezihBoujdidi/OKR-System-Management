import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SubscriptionComponent } from './components/subscription.component';
import { UpgradePlansComponent } from './components/upgrade-plans.component';

const routes: Routes = [
  {
    path: '',
    component: SubscriptionComponent
  },
  {
    path: 'upgrade',
    component: UpgradePlansComponent
  },
  {
    path: 'history',
    // This route would be for billing history, redirecting to the main subscription page for now
    redirectTo: '',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class SubscriptionRoutingModule { } 