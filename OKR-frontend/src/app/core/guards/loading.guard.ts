import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class LoadingGuard implements CanActivate {
  // Define routes that need the loading delay
  private delayedRoutes = ['/login', '/okrs', '/signup'];

  async canActivate(route: ActivatedRouteSnapshot): Promise<boolean> {
    const fullPath = this.getFullPath(route);
    
    // Only add delay for specific routes
    if (this.delayedRoutes.some(route => fullPath.startsWith(route))) {
      await new Promise(resolve => setTimeout(resolve, 800));
    }
    
    return true;
  }

  private getFullPath(route: ActivatedRouteSnapshot): string {
    let path = '';
    while (route.parent) {
      path = `/${route.routeConfig?.path}${path}`;
      route = route.parent;
    }
    return path;
  }
} 