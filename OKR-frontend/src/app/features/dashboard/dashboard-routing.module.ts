import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard.component';
import { OrganizationAdminDashboardComponent } from './components/organization-admin-dashboard/organization-admin-dashboard.component';
import { TeamManagerDashboardComponent } from './components/team-manager-dashboard/team-manager-dashboard.component';
import { CollaboratorDashboardComponent } from './components/collaborator-dashboard/collaborator-dashboard.component';
import { DashboardIndexComponent } from './components/dashboard-index/dashboard-index.component';
import { RoleType } from '../../models/user.interface';
import { SuperAdminDashboardComponent } from './components/super-admin-dashboard/super-admin-dashboard.component';

const routes: Routes = [
  {
    path: '',
    children: [
      {
        path: '',
        component: DashboardIndexComponent
      },
      {
        path: 'organizationAdmin',
        component: OrganizationAdminDashboardComponent,
        data: { roles: [RoleType.OrganizationAdmin] }
      },
      {
        path: 'teamManager',
        component: TeamManagerDashboardComponent,
        data: { roles: [RoleType.TeamManager] }
      },
      {
        path: 'collaborator',
        component: CollaboratorDashboardComponent,
        data: { roles: [RoleType.Collaborator] }
      },
      {
        path: 'superAdmin',
        component: SuperAdminDashboardComponent,
        data: { roles: [RoleType.SuperAdmin] }
      }
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DashboardRoutingModule { } 