import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HomeIndexComponent } from './components/home-index/home-index.component';
import { HomeOrganizationAdminComponent } from './components/home-organization-admin/home-organization-admin.component';
import { HomeTeamManagerComponent } from './components/home-team-manager/home-team-manager.component';
import { HomeCollaboratorComponent } from './components/home-collaborator/home-collaborator.component';
import { TimelineModule } from '../../shared/timeline/timeline.module';
import { DrawerModule } from '../../shared/drawer/drawer.module';
import { RouterModule } from '@angular/router';
import { LoadingOverlayModule } from '../../shared/loading-overlay/loading-overlay.module';
import { HomeRoutingModule } from './home-routing.module';
import { HomeSuperAdminComponent } from './components/home-super-admin/home-super-admin.component';

@NgModule({
  declarations: [
    HomeIndexComponent,
    HomeOrganizationAdminComponent,
    HomeTeamManagerComponent,
    HomeCollaboratorComponent,
    HomeSuperAdminComponent
  ],
  imports: [
    CommonModule,
    TimelineModule,
    DrawerModule,
    RouterModule,
    LoadingOverlayModule,
    HomeRoutingModule,
    CommonModule
  ]
})
export class HomeModule { } 