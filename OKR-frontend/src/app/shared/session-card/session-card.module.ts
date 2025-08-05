import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SessionCardComponent } from './components/session-card.component';

@NgModule({
  declarations: [SessionCardComponent],
  imports: [
    CommonModule
  ],
  exports: [SessionCardComponent]
})
export class SessionCardModule { }