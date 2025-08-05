import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ConfirmationModalComponent } from './components/confirmation-modal.component';
import { ModalComponent } from '../modal/modal.component';

@NgModule({
  declarations: [ConfirmationModalComponent],
  imports: [
    CommonModule,
    ModalComponent
  ],
  exports: [ConfirmationModalComponent]
})
export class ConfirmationModalModule { }