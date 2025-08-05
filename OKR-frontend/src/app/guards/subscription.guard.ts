import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError, switchMap, tap } from 'rxjs/operators';
import { AuthStateService } from '../services/auth-state.service';
import { OKRSessionService } from '../services/okr-session.service';
import { TeamService } from '../services/team.service';
import { RoleType } from '../models/role-type.enum';

@Injectable({ providedIn: 'root' })
export class SubscriptionGuard implements CanActivate {
  constructor(
    private router: Router,
    private authStateService: AuthStateService
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean | UrlTree> {
    const userRole = this.authStateService.getUserRole();
    const currentUrl = state.url;
    
    console.log('SubscriptionGuard: Checking access for URL:', currentUrl);
    console.log('SubscriptionGuard: User role:', userRole);
    
    // Check if the path is '/subscription/upgrade'
    if (currentUrl.includes('/subscription/upgrade')) {
      // Allow access for both SuperAdmin and OrganizationAdmin
      if (userRole === RoleType.SuperAdmin || userRole === RoleType.OrganizationAdmin) {
        console.log('SubscriptionGuard: Access granted to /subscription/upgrade');
        return of(true);
      } else {
        console.log('SubscriptionGuard: Access denied to /subscription/upgrade - not SuperAdmin or OrganizationAdmin');
        return of(this.router.parseUrl('/unauthorized'));
      }
    } 
    // For all other subscription routes, only SuperAdmin can access
    else {
      if (userRole === RoleType.SuperAdmin) {
        console.log('SubscriptionGuard: SuperAdmin access granted');
        return of(true);
      } else {
        console.log('SubscriptionGuard: Access denied - not SuperAdmin');
        return of(this.router.parseUrl('/unauthorized'));
      }
    }
  }
} 