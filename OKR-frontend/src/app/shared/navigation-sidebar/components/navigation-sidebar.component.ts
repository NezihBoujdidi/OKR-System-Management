import { Component, OnInit, Output, EventEmitter, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter, takeUntil } from 'rxjs/operators';
import { AuthStateService } from '../../../services/auth-state.service';
import { Subject } from 'rxjs';
import { UserDetailsWithRole } from '../../../models/user.interface';
import { RoleType } from '../../../models/role-type.enum';

interface User {
  name: string;
  email: string;
  imageUrl?: string;
}

@Component({
  selector: 'app-navigation-sidebar',
  templateUrl: './navigation-sidebar.component.html'
})
export class NavigationSidebarComponent implements OnInit, OnDestroy {
  activeRoute: string = '';
  @Output() buttonClicked = new EventEmitter<string>();
  showLoadingOverlay = false;
  isSuperAdmin = false;
  isTeamManagerORcollaborator = false;
  
  private destroy$ = new Subject<void>();
  currentUser: User = {
    name: 'John Doe',
    email: 'john@example.com'
  };

  constructor(
    private router: Router,
    private authState: AuthStateService
  ) {}

  ngOnInit() {
    // Set initial active route
    const initialPath = this.router.url.split('/')[1];
    this.setActiveRoute(initialPath);

    // Subscribe to route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      takeUntil(this.destroy$)
    ).subscribe((event: any) => {
      const path = event.url.split('/')[1];
      this.setActiveRoute(path);
    });

    // Subscribe to the current user from AuthStateService
    this.authState.getUser$().pipe(
      takeUntil(this.destroy$)
    ).subscribe(user => {
      if (user) {
        this.currentUser = {
          name: `${user.firstName} ${user.lastName}`,
          email: user.email,
          imageUrl: user.profilePictureUrl
        };
        
        // Check if user is SuperAdmin
        this.isSuperAdmin = user.role === RoleType.SuperAdmin;
        this.isTeamManagerORcollaborator = ( user.role === RoleType.TeamManager || user.role === RoleType.Collaborator);
        console.log("User role: ", user.role);
        console.log("isSuperAdmin: ", this.isSuperAdmin);
        console.log("isTeamManagerORcollaborator: ", this.isTeamManagerORcollaborator);
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setActiveRoute(path: string) {
    if (path === 'okrs') {
      this.activeRoute = 'okrs';
    } else if (path === 'employees' || path === 'teams') {
      this.activeRoute = 'employees';
    } else if (path === 'home' || path === 'dashboard') {
      this.activeRoute = 'home';
    } else if (path === 'users' || path === 'organization' || path === 'subscription') {
      this.activeRoute = 'manage';
    } else if (path === 'chat') {
      this.activeRoute = 'chat';
    } else if (path === 'meetings') {
      this.activeRoute = 'meetings';
    } else {
      this.activeRoute = path || 'home';
    }
  }

  async navigate(route: string) {
    if (route === 'chat') {
      this.showLoadingOverlay = true;
      
      // Add a small delay for the animation
      await new Promise(resolve => setTimeout(resolve, 800));
    }
    
    // Split the route if it contains a slash (for nested routes)
    const routeParts = route.split('/');
    const mainRoute = routeParts[0];
    
    if (mainRoute === 'home') {
      // Check user role for proper home/dashboard navigation
      const currentUser = this.authState.getCurrentUser();
      if (currentUser) {
        // Navigate to dashboard for users that have access to dashboards
        this.router.navigate(['/dashboard']);
      } else {
        this.router.navigate(['/home']);
      }
    } else if (mainRoute === 'okrs') {
      this.router.navigate(['/okrs']);
    } else if (mainRoute === 'manage') {
      this.router.navigate(['/users']);
    } else if (mainRoute === 'employees') {
      this.router.navigate(['/employees']);
    } else if (mainRoute === 'profile') {
      this.router.navigate(['/profile']);
    } else if (mainRoute === 'meetings') {
      this.router.navigate(['/meetings']);
    } else if (mainRoute === 'subscription') {
      // Handle subscription routes with proper nested paths
      if (routeParts.length > 1) {
        this.router.navigate([`/subscription/${routeParts[1]}`]);
      } else {
        this.router.navigate(['/subscription']);
      }
    } else {
      this.router.navigate([`/${route}`]);
    }
  }
}