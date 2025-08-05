import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginComponent } from './components/login/login.component';
import { SharedModule } from '../../shared/shared.module';
import { LoginRoutingModule } from './login-routing.module';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { LoadingOverlayModule } from '@shared/loading-overlay/loading-overlay.module';
/* import { LoaderComponent } from '@shared/loader/components/loader.component'; */

@NgModule({
  declarations: [
    LoginComponent,
    ForgotPasswordComponent,
    ResetPasswordComponent,
    //LoaderComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    // ModalModule,
    SharedModule,
    LoginRoutingModule,
    LoadingOverlayModule
  ]
})
export class LoginModule { }