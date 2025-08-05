import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DataTableComponent } from './data-table.component';
import { DataTableMessageComponent } from './data-table-message.component';
import { TableDataService } from './table-data.service';

@NgModule({
  declarations: [
    DataTableComponent,
    DataTableMessageComponent
  ],
  imports: [
    CommonModule,
    FormsModule
  ],
  exports: [
    DataTableComponent,
    DataTableMessageComponent
  ],
  providers: [
    TableDataService
  ]
})
export class DataTableModule { } 