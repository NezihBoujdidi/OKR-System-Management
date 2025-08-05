import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EmployeesComponent } from './components/employees.component';
import { SharedModule } from '../../shared/shared.module';
import { EmployeesRoutingModule } from './employees-routing.module';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { FilterButtonModule } from '../../shared/filter-button/filter-button.module';
import { SortButtonModule } from '../../shared/sort-button/sort-button.module';

@NgModule({
  declarations: [EmployeesComponent],
  imports: [
    CommonModule,
    SharedModule,
    EmployeesRoutingModule,
    ReactiveFormsModule,
    FormsModule,
    FilterButtonModule,
    SortButtonModule
  ]
})
export class EmployeesModule { } 