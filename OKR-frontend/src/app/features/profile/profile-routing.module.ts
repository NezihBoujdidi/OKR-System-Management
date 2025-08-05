import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProfileComponent } from './components/profile/profile.component';
import { ManageAccountComponent } from './components/manage-account/manage-account.component';
import { PasswordComponent } from './components/password/password.component';
import { GuidelinesComponent } from './components/guidelines/guidelines.component';

const routes: Routes = [
  {
    path: '',
    component: ProfileComponent
  },
  {
    path: 'account',
    component: ManageAccountComponent
  },
  {
    path: 'password',
    component: PasswordComponent
  },
  {
    path: 'guidelines',
    component: GuidelinesComponent
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProfileRoutingModule { } 