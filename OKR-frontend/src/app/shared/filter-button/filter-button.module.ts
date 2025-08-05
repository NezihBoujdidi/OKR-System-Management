import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FilterButtonComponent } from './components/filter-button.component';
import { SharedModule } from '../shared.module';

@NgModule({
  declarations: [
    FilterButtonComponent
  ],
  imports: [
    CommonModule,
    SharedModule
  ],
  exports: [
    FilterButtonComponent
  ]
})
export class FilterButtonModule { } 