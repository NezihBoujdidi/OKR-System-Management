import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TeamsRoutingModule } from './teams-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { OrganizationAdminTeamsComponent } from './components/organization-admin-teams/organization-admin-teams.component';
import { TeamManagerTeamsComponent } from './components/team-manager-teams/team-manager-teams.component';
import { CollaboratorTeamsComponent } from './components/collaborator-teams/collaborator-teams.component';
import { TeamDetailsModalComponent } from './components/team-details-modal/team-details-modal.component';
import { SuperAdminTeamsComponent } from './components/super-admin-teams/super-admin-teams.component';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';

@NgModule({
  declarations: [
    OrganizationAdminTeamsComponent,
    TeamManagerTeamsComponent,
    CollaboratorTeamsComponent,
    TeamDetailsModalComponent,
    SuperAdminTeamsComponent
  ],
  imports: [
    CommonModule,
    TeamsRoutingModule,
    SharedModule,
    ReactiveFormsModule,
    FormsModule
  ]
})
export class TeamsModule { } 