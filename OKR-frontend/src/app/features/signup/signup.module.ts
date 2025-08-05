import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignupComponent } from './components/signup.component';
import { SharedModule } from '../../shared/shared.module';
import { SignupRoutingModule } from './signup-routing.module';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';



@NgModule({
  declarations: [SignupComponent],
  imports: [
    CommonModule,
    SharedModule,
    SignupRoutingModule,
    ReactiveFormsModule,
    FormsModule
  ]
})
export class SignupModule { } 