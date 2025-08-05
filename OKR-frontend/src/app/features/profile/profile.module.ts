import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProfileRoutingModule } from './profile-routing.module';
import { ProfileComponent } from './components/profile/profile.component';
import { ManageAccountComponent } from './components/manage-account/manage-account.component';
import { PasswordComponent } from './components/password/password.component';
import { SharedModule } from '../../shared/shared.module';
import { ReactiveFormsModule } from '@angular/forms';
import { GuidelinesComponent } from './components/guidelines/guidelines.component';
import { LineBreaksPipe } from '../../shared/pipes/line-breaks.pipe';

@NgModule({
  declarations: [
    ProfileComponent,
    ManageAccountComponent,
    PasswordComponent,
    GuidelinesComponent,
    LineBreaksPipe
  ],
  imports: [
    CommonModule,
    ProfileRoutingModule,
    SharedModule,
    ReactiveFormsModule
  ]
})
export class ProfileModule { } 