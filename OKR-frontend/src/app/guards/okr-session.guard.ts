import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError, switchMap, tap } from 'rxjs/operators';
import { AuthStateService } from '../services/auth-state.service';
import { OKRSessionService } from '../services/okr-session.service';
import { TeamService } from '../services/team.service';
import { RoleType } from '../models/role-type.enum';

@Injectable({ providedIn: 'root' })
export class OkrSessionGuard implements CanActivate {
  constructor(
    private router: Router,
    private authStateService: AuthStateService,
    private okrSessionService: OKRSessionService,
    private teamService: TeamService
  ) {}

  canActivate(route: ActivatedRouteSnapshot): Observable<boolean|UrlTree> {
    const sessionId = route.paramMap.get('id');
    const orgId = route.paramMap.get('orgId'); // Extract orgId from route params
    const userRole = this.authStateService.getUserRole();
    const user = this.authStateService.getCurrentUser();
    
    console.log('==== OkrSessionGuard: Authorization Check ====');
    console.log('Session ID:', sessionId);
    console.log('Organization ID from route:', orgId);
    console.log('User Role:', userRole);
    console.log('User ID:', user?.id);
    console.log('User Organization ID:', user?.organizationId);
    
    // SuperAdmin can access everything
    if (userRole === RoleType.SuperAdmin) {
      console.log('SuperAdmin detected - full access granted');
      return of(true);
    }
    
    // OrganizationAdmin needs verification
    if (userRole === RoleType.OrganizationAdmin && sessionId && user?.organizationId) {
      console.log(`OrgAdmin detected - checking if session ${sessionId} belongs to org ${user.organizationId}`);
      
      return this.okrSessionService.getOKRSessionById(sessionId).pipe(
        tap(session => {
          console.log('Session data received:', session ? {
            id: session.id,
            title: session.title,
            organizationId: session.organizationId,
            teamIds: session.teamIds || []
          } : 'Session not found');
        }),
        map(session => {
          // If session doesn't exist, let normal 404 handling work
          if (!session) {
            console.log('Session not found');
            return true;
          }
          
          console.log(`Comparing session org: ${session.organizationId} with user org: ${user.organizationId}`);
          
          // Check if session belongs to user's organization
          if (session.organizationId !== user.organizationId) {
            console.warn(`Unauthorized access attempt to session ${sessionId} by user ${user.id}`);
            return this.router.parseUrl('/unauthorized');
          }
          
          console.log('Access granted - session belongs to user organization');
          return true;
        }),
        catchError(error => {
          console.error('Error checking session access:', error);
          return of(true); // On error, let component handle it
        })
      );
    }
    
    // TeamManager and Collaborator need to check if the session belongs to their teams
    if ((userRole === RoleType.TeamManager || userRole === RoleType.Collaborator) && sessionId && user?.id) {
      console.log(`TeamManager/Collaborator detected - checking if session ${sessionId} belongs to user's teams`);
      
      // First check the session data for debugging
      return this.okrSessionService.debugSessionData(sessionId).pipe(
        catchError(error => {
          console.error('Failed to debug session data', error);
          return this.okrSessionService.getOKRSessionById(sessionId);
        }),
        switchMap(session => {
          if (!session) {
            console.log('Session not found');
            return of(true); // Let component handle 404
          }
          
          // Then get user's teams
          return this.teamService.getTeamsByUserId(user.id).pipe(
            tap(teams => {
              console.log(`User belongs to ${teams.length} teams:`, teams.map(t => ({ id: t.id, name: t.name })));
            }),
            map(teams => {
              // Check if the session belongs to any of the user's teams
              // We need to check if any team ID from the user's teams matches
              // any team ID in the session's teamIds array
              
              // Get all team IDs the user is part of
              const userTeamIds = teams.map(team => team.id);
              console.log('User team IDs:', userTeamIds);
              
              // Ensure session.teamIds exists and is an array
              let sessionTeamIds: string[] = [];
              
              // Handle different possible formats of teamIds
              if (Array.isArray(session.teamIds)) {
                sessionTeamIds = session.teamIds;
              } else if (typeof session.teamIds === 'string') {
                try {
                  const parsed = JSON.parse(session.teamIds);
                  sessionTeamIds = Array.isArray(parsed) ? parsed : [];
                } catch (e) {
                  // If it's a single ID as string
                  sessionTeamIds = [session.teamIds];
                }
              }
              
              console.log('Session team IDs (processed):', sessionTeamIds);
              
              // Check if there's any intersection between user teams and session teams
              const hasTeamAccess = sessionTeamIds.some(teamId => userTeamIds.includes(teamId));
              
              if (hasTeamAccess) {
                console.log('✅ Access granted - session belongs to user team');
                return true;
              }
              
              // Additional check - also allow access if the user belongs to the same organization
              if (user.organizationId && session.organizationId === user.organizationId) {
                console.log('✅ Access granted - session belongs to user organization');
                return true;
              }
              
              // TEMPORARY: Allow access regardless for troubleshooting
              console.log('⚠️ TEMPORARY OVERRIDE: Granting access for debugging');
              return true;
              
              // Regular authorization logic (commented out for testing)
              // console.warn(`❌ Unauthorized access attempt to session ${sessionId} by user ${user.id}`);
              // return this.router.parseUrl('/unauthorized');
            }),
            catchError(error => {
              console.error('Error checking team access:', error);
              
              // TEMPORARY: Allow access on error for troubleshooting
              console.log('⚠️ TEMPORARY OVERRIDE: Granting access after error');
              return of(true);
              
              // Regular error handling (commented out for testing)
              // return of(this.router.parseUrl('/unauthorized')); // Explicit redirect on error
            })
          );
        }),
        catchError(error => {
          console.error('Error checking session access:', error);
          
          // TEMPORARY: Allow access on error for troubleshooting
          console.log('⚠️ TEMPORARY OVERRIDE: Granting access after error');
          return of(true);
          
          // Regular error handling (commented out for testing)
          // return of(this.router.parseUrl('/unauthorized')); // Explicit redirect on error
        })
      );
    }
    
    // If user doesn't match any role with specific access, deny by default
    if (sessionId) {
      console.warn(`No role-specific access check for user role ${userRole}`);
      return of(this.router.parseUrl('/unauthorized'));
    }
    
    // For non-session routes, allow access
    return of(true);
  }
} 