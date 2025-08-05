import { NgModule, inject } from '@angular/core';
import { RouterModule, Routes, ActivatedRoute } from '@angular/router';
import { OrganizationAdminTeamsComponent } from './components/organization-admin-teams/organization-admin-teams.component';
import { TeamManagerTeamsComponent } from './components/team-manager-teams/team-manager-teams.component';
import { CollaboratorTeamsComponent } from './components/collaborator-teams/collaborator-teams.component';
import { SuperAdminTeamsComponent } from './components/super-admin-teams/super-admin-teams.component';
import { RoleGuard } from '../../guards/role.guard';
import { RoleType } from '../../models/role-type.enum';
import { AuthStateService } from '../../services/auth-state.service';
import { Router } from '@angular/router';

// Function to redirect based on user role - replaces TeamsIndexComponent logic
export const roleBasedTeamRedirect = () => {
  const authState = inject(AuthStateService);
  const router = inject(Router);
  const route = inject(ActivatedRoute);
  
  const currentUser = authState.getCurrentUser();
  const url = router.url;
  
  console.log('🔍 Teams routing - Current URL:', url);
  console.log('🔍 Teams routing - Current user:', currentUser?.id, currentUser?.email, currentUser?.role);
  
  // Check if we're on an organization-specific route by examining the URL directly
  if (url.includes('/OrgTeams/')) {
    console.log('🔍 Teams routing - On organization teams route. No redirect needed.');
    // Don't redirect if on the org teams route
    return true;
  }
  
  if (!currentUser) {
    console.error('🚫 Teams routing - No user found, redirecting to login');
    router.navigate(['/login']);
    return false;
  }
  
  console.log('🔍 Teams routing - Role based redirect for:', currentUser.role);
  
  switch (currentUser.role) {
    case RoleType.OrganizationAdmin:
      console.log('🔄 Teams routing - Redirecting to organization admin teams');
      router.navigate(['/teams/organizationAdmin']);
      break;
    case RoleType.TeamManager:
      console.log('🔄 Teams routing - Redirecting to team manager teams');
      router.navigate(['/teams/teamManager']);
      break;
    case RoleType.Collaborator:
      console.log('🔄 Teams routing - Redirecting to collaborator teams');
      router.navigate(['/teams/collaborator']);
      break;
    case RoleType.SuperAdmin:
      console.log('🔄 Teams routing - Redirecting to super admin teams');
      router.navigate(['/teams/superAdmin']);
      break;
    default:
      console.error('❌ Teams routing - Unknown role, redirecting to default view');
      router.navigate(['/teams/organizationAdmin']);
      break;
  }
  
  return false;
};

const routes: Routes = [
  // New route for organization-specific teams
  {
    path: ':id/teams',
    component: SuperAdminTeamsComponent,
    canActivate: [RoleGuard],
    data: { roles: [RoleType.SuperAdmin] }
  },
  
  // Handle the base path with role-based redirection
  {
    path: '',
    canActivate: [() => {
      console.log('🔄 Teams routing - Handling empty path with role-based redirect');
      return roleBasedTeamRedirect();
    }],
    component: SuperAdminTeamsComponent // Fallback component that should never render
  },
  
  // Regular team routes
  {
    path: 'organizationAdmin',
    component: OrganizationAdminTeamsComponent,
    canActivate: [RoleGuard],
    data: { roles: [RoleType.OrganizationAdmin, RoleType.SuperAdmin] }
  },
  {
    path: 'teamManager',
    component: TeamManagerTeamsComponent,
    canActivate: [RoleGuard],
    data: { roles: [RoleType.TeamManager, RoleType.SuperAdmin] }
  },
  {
    path: 'collaborator',
    component: CollaboratorTeamsComponent,
    canActivate: [RoleGuard],
    data: { roles: [RoleType.Collaborator, RoleType.SuperAdmin] }
  },
  {
    path: 'superAdmin',
    component: SuperAdminTeamsComponent,
    canActivate: [RoleGuard],
    data: { roles: [RoleType.SuperAdmin] }
  },
  {
    path: '**',
    canActivate: [() => {
      console.log('🔄 Teams routing - Catch-all route with role-based redirect');
      return roleBasedTeamRedirect();
    }],
    component: SuperAdminTeamsComponent // Fallback component that should never render
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class TeamsRoutingModule { } 