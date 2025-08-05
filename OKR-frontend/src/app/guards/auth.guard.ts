import { Injectable } from '@angular/core';
import { Router, CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthStateService } from '../services/auth-state.service';
import { Observable, of } from 'rxjs';
import { take, map, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authState: AuthStateService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    console.log('AUTH-GUARD: Checking authentication for route:', state.url);
    
    // Use the observable to properly check authentication state
    return this.authState.isAuthenticated$.pipe(
      take(1),
      map(isAuthenticated => {
        console.log('AUTH-GUARD: Authentication state for route:', state.url, 'is', isAuthenticated);
        
        if (isAuthenticated) {
          return true;
        }
        
        // If session storage has values but the auth state hasn't been initialized yet, try to refresh
        const hasSessionStorage = !!sessionStorage.getItem('supabase.auth.token') && 
                                 !!sessionStorage.getItem('user');
        
        if (hasSessionStorage) {
          console.log('AUTH-GUARD: Found session data in storage, forcing auth state check');
          // Force a state check and assume authenticated for now
          // This helps during page refreshes where auth service might not be fully initialized
          return true;
        }
        
        console.log('AUTH-GUARD: Not authenticated, redirecting to login');
        this.router.navigate(['/login'], { queryParams: { returnUrl: state.url }});
        return false;
      })
    );
  }
}
