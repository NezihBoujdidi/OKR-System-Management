import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeIndexComponent } from './components/home-index/home-index.component';
import { HomeOrganizationAdminComponent } from './components/home-organization-admin/home-organization-admin.component';
import { HomeTeamManagerComponent } from './components/home-team-manager/home-team-manager.component';
import { HomeCollaboratorComponent } from './components/home-collaborator/home-collaborator.component';
import { RoleType } from '../../models/role-type.enum';
import { HomeSuperAdminComponent } from './components/home-super-admin/home-super-admin.component';

const routes: Routes = [
  {
    path: '',
    component: HomeIndexComponent
  },
  {
    path: 'superAdmin',
    component: HomeSuperAdminComponent,
    data: { roles: [RoleType.SuperAdmin] }
  },
  {
    path: 'organizationAdmin',
    component: HomeOrganizationAdminComponent,
    data: { roles: [RoleType.OrganizationAdmin] }
  },
  {
    path: 'teamManager',
    component: HomeTeamManagerComponent,
    data: { roles: [RoleType.TeamManager] }
  },
  {
    path: 'collaborator',
    component: HomeCollaboratorComponent,
    data: { roles: [RoleType.Collaborator] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class HomeRoutingModule { } 