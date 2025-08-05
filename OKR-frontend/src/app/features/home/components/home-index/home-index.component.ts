import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStateService } from '../../../../services/auth-state.service';
import { RoleType } from '../../../../models/role-type.enum';

@Component({
  selector: 'app-home-index',
  template: '<div class="flex items-center justify-center h-screen"><div class="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-accent"></div></div>'
})
export class HomeIndexComponent implements OnInit {
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
        this.router.navigate(['/home/organizationAdmin']);
        break;
      case RoleType.TeamManager:
        this.router.navigate(['/home/teamManager']);
        break;
      case RoleType.Collaborator:
        this.router.navigate(['/home/collaborator']);
        break;
      case RoleType.SuperAdmin:
        this.router.navigate(['/home/superAdmin']);
        break;
      default:
        console.error('Unknown role, redirecting to default view');
        this.router.navigate(['/unauthorized']);
        break;
    }
  }
} 