import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, from } from 'rxjs';
import { UserDetails, UserDetailsWithRole } from '../models/user.interface';
import { AuthResponse } from '../models/auth.interface';
import { RoleType } from '../models/role-type.enum';
import { distinctUntilChanged, map, shareReplay, tap } from 'rxjs/operators';
import { Session, User } from '@supabase/supabase-js';

@Injectable({
  providedIn: 'root'
})
export class AuthStateService {
  private readonly STORAGE_KEYS = {
    SESSION: 'supabase.auth.token',
    USER: 'user'
  };

  private currentUserSubject = new BehaviorSubject<UserDetailsWithRole | null>(null);
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  private sessionSubject = new BehaviorSubject<Session | null>(null);
  private loggingOutSubject = new BehaviorSubject<boolean>(false);
  private initialized = false;
  
  // Cache the user role to avoid repeated computation and logging
  private cachedUserRole: RoleType | null = null;
  private shouldLogRoleAccess = true; // Control logging frequency

  // Public observables with distinctUntilChanged to prevent unnecessary emissions
  currentUser$ = this.currentUserSubject.asObservable().pipe(
    distinctUntilChanged(),
    shareReplay(1)
  );
  
  isAuthenticated$ = this.isAuthenticatedSubject.asObservable().pipe(
    distinctUntilChanged(),
    shareReplay(1)
  );
  
  session$ = this.sessionSubject.asObservable().pipe(
    distinctUntilChanged(),
    shareReplay(1)
  );
  
  loggingOut$ = this.loggingOutSubject.asObservable().pipe(
    distinctUntilChanged(),
    shareReplay(1)
  );

  constructor() {
    console.log('AUTH-STATE: AuthStateService constructor called');
    // Check sessionStorage on service initialization
    this.checkAuthState();
  }

  private checkAuthState() {
    console.log('AUTH-STATE: Checking auth state');
    
    try {
      const sessionStr = sessionStorage.getItem(this.STORAGE_KEYS.SESSION);
      const userStr = sessionStorage.getItem(this.STORAGE_KEYS.USER);
      
      console.log('AUTH-STATE: Session storage check - Session exists:', !!sessionStr, 'User exists:', !!userStr);
      
      if (sessionStr && userStr) {
        const session = JSON.parse(sessionStr);
        const user = JSON.parse(userStr);
        
        // Check if session is expired - use a buffer of 5 minutes to be safe
        const now = new Date();
        const expiryDate = new Date(session?.expires_at * 1000); // Convert to milliseconds if needed
        const bufferTime = 5 * 60 * 1000; // 5 minutes in milliseconds
        
        console.log('AUTH-STATE: Session expiry check - Current time:', now.toISOString(), 
                    'Expiry time:', expiryDate.toISOString(),
                    'Difference (ms):', expiryDate.getTime() - now.getTime());
        
        if (session?.expires_at && (expiryDate.getTime() - bufferTime < now.getTime())) {
          console.log('AUTH-STATE: Session expired or about to expire, clearing auth state');
          this.clearAuthState();
          return;
        }

        console.log('AUTH-STATE: Setting auth state from stored data, user role:', user?.role);
        this.currentUserSubject.next(user);
        this.isAuthenticatedSubject.next(true);
        this.sessionSubject.next(session);
        this.initialized = true;
        
        // Cache the role
        this.cachedUserRole = user?.role || null;
      } else {
        console.log('AUTH-STATE: No stored session found');
        this.initialized = true;
      }
    } catch (error) {
      console.error('AUTH-STATE: Error checking auth state:', error);
      this.clearAuthState();
      this.initialized = true;
    }
  }

  setAuthState(session: Session, user: UserDetailsWithRole) {
    console.log('AUTH-STATE: Setting auth state with user role:', user.role);
    
    try {
      // Store in sessionStorage
      const sessionJson = JSON.stringify(session);
      const userJson = JSON.stringify(user);
      
      sessionStorage.setItem(this.STORAGE_KEYS.SESSION, sessionJson);
      sessionStorage.setItem(this.STORAGE_KEYS.USER, userJson);
      
      // Update subjects
      this.currentUserSubject.next(user);
      this.isAuthenticatedSubject.next(true);
      this.sessionSubject.next(session);
      this.initialized = true;
      
      // Cache the role
      this.cachedUserRole = user?.role || null;
      
      console.log('AUTH-STATE: Auth state successfully set');
    } catch (error) {
      console.error('AUTH-STATE: Error setting auth state:', error);
    }
  }

  startLogout() {
    console.log('AuthStateService: Starting logout process');
    this.loggingOutSubject.next(true);
  }
  
  finishLogout() {
    console.log('AuthStateService: Finishing logout process');
    this.loggingOutSubject.next(false);
  }
  
  isLoggingOut(): boolean {
    return this.loggingOutSubject.value;
  }

  clearAuthState() {
    console.log('AuthStateService: Clearing auth state...');
    
    try {
      // Clear sessionStorage - try different approaches to ensure complete cleanup
      // 1. Remove specific keys
      sessionStorage.removeItem(this.STORAGE_KEYS.SESSION);
      sessionStorage.removeItem(this.STORAGE_KEYS.USER);
      
      // 2. Try to clear entire sessionStorage
      sessionStorage.clear();
      
      // 3. Set to empty values as fallback
      sessionStorage.setItem(this.STORAGE_KEYS.SESSION, '');
      sessionStorage.setItem(this.STORAGE_KEYS.USER, '');
      
      // Log session storage state after clearing
      console.log('Session storage after clearing:', {
        sessionLength: sessionStorage.length,
        sessionKeys: Object.keys(sessionStorage)
      });
      
      // Reset subjects
      this.currentUserSubject.next(null);
      console.log('currentUserSubject set to null');
      this.isAuthenticatedSubject.next(false);
      console.log('isAuthenticatedSubject set to false');
      this.sessionSubject.next(null);
      console.log('sessionSubject set to null');
      
      // Clear the role cache
      this.cachedUserRole = null;
      
      // Double-check after a small delay
      setTimeout(() => {
        if (sessionStorage.getItem(this.STORAGE_KEYS.SESSION) || 
            sessionStorage.getItem(this.STORAGE_KEYS.USER)) {
          console.warn('AuthStateService: Session storage keys still exist after timeout - forcing clear again');
          sessionStorage.clear();
          sessionStorage.removeItem(this.STORAGE_KEYS.SESSION);
          sessionStorage.removeItem(this.STORAGE_KEYS.USER);
        }
      }, 100);
    } catch (error) {
      console.error('Error in clearAuthState:', error);
      // Try one more time with a basic approach
      try {
        sessionStorage.clear();
      } catch (e) {
        console.error('Final attempt to clear sessionStorage failed:', e);
      }
    }
  }

  getCurrentUser(): UserDetailsWithRole | null {
    // Make sure we've initialized before returning a value
    if (!this.initialized) {
      console.log('AUTH-STATE: getCurrentUser called before initialization');
      this.checkAuthState();
    }
    return this.currentUserSubject.value;
  }

  isAuthenticated(): boolean {
    // Make sure we've initialized before returning a value
    if (!this.initialized) {
      console.log('AUTH-STATE: isAuthenticated called before initialization');
      this.checkAuthState();
    }
    return this.isAuthenticatedSubject.value;
  }

  getUserRole(): RoleType | null {
    // Check if we have a cached role first
    if (this.cachedUserRole !== null) {
      // Only log once in a while to reduce console spam
      if (this.shouldLogRoleAccess) {
        console.log('AUTH-STATE: getUserRole returning (cached):', this.cachedUserRole);
        // Disable logging for a short period
        this.shouldLogRoleAccess = false;
        setTimeout(() => {
          this.shouldLogRoleAccess = true;
        }, 5000); // Only log once every 5 seconds
      }
      return this.cachedUserRole;
    }
    
    // Make sure we've initialized before returning a value
    if (!this.initialized) {
      console.log('AUTH-STATE: getUserRole called before initialization');
      this.checkAuthState();
    }
    
    // Get role from current user
    const role = this.currentUserSubject.value?.role || null;
    
    // Cache the role for future calls
    this.cachedUserRole = role;
    
    // Only log if logging is enabled
    if (this.shouldLogRoleAccess) {
      console.log('AUTH-STATE: getUserRole returning:', role);
      // Disable logging for a short period
      this.shouldLogRoleAccess = false;
      setTimeout(() => {
        this.shouldLogRoleAccess = true;
      }, 5000); // Only log once every 5 seconds
    }
    
    return role;
  }

  hasRole(role: RoleType): boolean {
    const userRole = this.getUserRole();
    return userRole === role;
  }

  hasAnyRole(roles: RoleType[]): boolean {
    const userRole = this.getUserRole();
    return userRole ? roles.includes(userRole) : false;
  }

  // Update user data only without changing session state
  updateUserData(user: UserDetailsWithRole) {
    console.log('DEBUG-NAV: Updating user data with role:', user.role);
    
    // Update in sessionStorage
    sessionStorage.setItem(this.STORAGE_KEYS.USER, JSON.stringify(user));
    
    // Update the subject
    this.currentUserSubject.next(user);
    
    // Update the role cache
    this.cachedUserRole = user?.role || null;
  }

  // Get session as Observable
  getSession$(): Observable<Session | null> {
    return this.session$;
  }

  // Get user as Observable
  getUser$(): Observable<UserDetailsWithRole | null> {
    return this.currentUser$.pipe(
      tap(user => console.log('DEBUG-NAV: currentUser$ emitted user with role:', user?.role || 'null'))
    );
  }

  // Get access token as Observable
  getAccessToken$(): Observable<string | null> {
    return this.session$.pipe(
      map(session => session?.access_token || null)
    );
  }

  // Get refresh token as Observable
  getRefreshToken$(): Observable<string | null> {
    return this.session$.pipe(
      map(session => session?.refresh_token || null)
    );
  }
} 