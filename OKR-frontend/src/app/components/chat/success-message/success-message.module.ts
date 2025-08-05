import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SuccessMessageComponent } from './success-message.component';

@NgModule({
  declarations: [
    SuccessMessageComponent
  ],
  imports: [
    CommonModule
  ],
  exports: [
    SuccessMessageComponent
  ]
})
export class SuccessMessageModule { }
