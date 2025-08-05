import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrganizationComponent } from './components/organization.component';
import { SharedModule } from '../../shared/shared.module';
import { FormsModule } from '@angular/forms';
import { OrganizationRoutingModule } from './organization-routing.module';
import { FilterButtonModule } from '../../shared/filter-button/filter-button.module';
import { SortButtonModule } from '../../shared/sort-button/sort-button.module';
import { LoadingOverlayModule } from '@shared/loading-overlay/loading-overlay.module';

@NgModule({
  declarations: [
    OrganizationComponent
  ],
  imports: [
    CommonModule,
    SharedModule,
    FormsModule,
    OrganizationRoutingModule,
    FilterButtonModule,
    SortButtonModule,
    LoadingOverlayModule
  ]
})
export class OrganizationModule { }