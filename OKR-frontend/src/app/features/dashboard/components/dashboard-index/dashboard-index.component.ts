import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStateService } from '../../../../services/auth-state.service';
import { RoleType } from '../../../../models/user.interface';

@Component({
  selector: 'app-dashboard-index',
  template: '<div class="h-screen w-full flex items-center justify-center"><div class="spinner"></div></div>', // Simple loading indicator
  styleUrls: ['./dashboard-index.component.scss']
})
export class DashboardIndexComponent implements OnInit {
  constructor(
    private router: Router,
    private authState: AuthStateService
  ) {}

  ngOnInit() {
    this.redirectBasedOnRole();
  }

  redirectBasedOnRole() {
    const currentUser = this.authState.getCurrentUser();
    
    if (!currentUser) {
      console.error('No user found, redirecting to login');
      this.router.navigate(['/login']);
      return;
    }

    console.log('Current user role:', currentUser.role);
    
    // Redirect based on role
    switch (currentUser.role) {
      case RoleType.OrganizationAdmin:
        this.router.navigate(['/dashboard/organizationAdmin']);
        break;
      case RoleType.TeamManager:
        this.router.navigate(['/dashboard/teamManager']);
        break;
      case RoleType.Collaborator:
        this.router.navigate(['/dashboard/collaborator']);
        break;
      case RoleType.SuperAdmin:
        // Super admins can see the org admin view
        this.router.navigate(['/dashboard/superAdmin']);
        break;
      default:
        console.error('Unknown role, redirecting to default view');
        this.router.navigate(['/unauthorized']);
        break;
    }
  }
} 