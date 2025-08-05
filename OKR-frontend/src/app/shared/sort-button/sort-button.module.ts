import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SortButtonComponent } from './components/sort-button.component';
import { SharedModule } from '../shared.module';

@NgModule({
  declarations: [
    SortButtonComponent
  ],
  imports: [
    CommonModule,
    SharedModule
  ],
  exports: [
    SortButtonComponent
  ]
})
export class SortButtonModule { } 