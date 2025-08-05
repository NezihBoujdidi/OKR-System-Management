import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DashboardLayoutComponent } from './dashboard-layout/dashboard-layout.component';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [
    DashboardLayoutComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    SharedModule
  ]
})
export class LayoutsModule { } 