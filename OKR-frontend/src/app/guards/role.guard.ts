import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthStateService } from '../services/auth-state.service';
import { RoleType } from '../models/role-type.enum';
import { Observable } from 'rxjs';
import { take, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class RoleGuard implements CanActivate {
  constructor(
    private authState: AuthStateService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    const requiredRoles = route.data['roles'] as RoleType[];
    console.log('ROLE-GUARD: Checking roles for route:', state.url, 'Required roles:', requiredRoles);
    
    return this.authState.currentUser$.pipe(
      take(1),
      map(user => {
        // First check if user is authenticated
        if (!user) {
          console.log('ROLE-GUARD: No authenticated user, redirecting to login');
          this.router.navigate(['/login'], { queryParams: { returnUrl: state.url }});
          return false;
        }
        
        // Then check if user has any of the required roles
        const userRole = user.role;
        const hasRequiredRole = requiredRoles.includes(userRole);
        
        console.log('ROLE-GUARD: User role:', userRole, 'Has required role:', hasRequiredRole);
        
        if (!hasRequiredRole) {
          console.log('ROLE-GUARD: User lacks required role, redirecting to unauthorized');
          this.router.navigate(['/unauthorized']);
          return false;
        }
        
        return true;
      })
    );

  }
} 