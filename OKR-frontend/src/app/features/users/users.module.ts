import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersComponent } from './components/users.component';
import { SharedModule } from '../../shared/shared.module';
import { UsersRoutingModule } from './users-routing.module';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { FilterButtonModule } from '../../shared/filter-button/filter-button.module';
import { SortButtonModule } from '../../shared/sort-button/sort-button.module';
import { LoadingOverlayModule } from '@shared/loading-overlay/loading-overlay.module';

@NgModule({
  declarations: [UsersComponent],
  imports: [
    CommonModule,
    SharedModule,
    UsersRoutingModule,
    ReactiveFormsModule,
    FormsModule,
    FilterButtonModule,
    SortButtonModule,
    LoadingOverlayModule
  ]
})
export class UsersModule { } 