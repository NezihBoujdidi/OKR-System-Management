import { Injectable, OnDestroy } from '@angular/core';
import { createClient, SupabaseClient, User, AuthResponse } from '@supabase/supabase-js';
import { environment } from '../../environments/environment';
import { Observable, from, throwError, Subject, switchMap, finalize, of } from 'rxjs';
import { map, catchError, takeUntil, tap } from 'rxjs/operators';
import { RegisterUserCommand } from '../models/auth.interface';
import { Router } from '@angular/router';
import { AuthStateService } from './auth-state.service';
import { UserService } from './user.service';
import { UserDetailsWithRole } from '../models/user.interface';
import { RoleType } from '../models/role-type.enum';
import { HttpClient } from '@angular/common/http';
import { User as SupabaseUser } from '@supabase/supabase-js';

declare global {
  interface Window {
    forceBlockAllRequests?: boolean;
  }
}

@Injectable({
  providedIn: 'root'
})
export class SupabaseAuthService implements OnDestroy {
  private supabase: SupabaseClient;
  private destroy$ = new Subject<void>();
  private lockTimeout = 5000; // 5 seconds timeout for lock acquisition

  constructor(
    private router: Router,
    private authState: AuthStateService,
    private userService: UserService,
    private http: HttpClient
  ) {
    // Initialize Supabase client with custom options
    this.supabase = createClient(
      environment.supabaseUrl,
      environment.supabaseKey,
      {
        auth: {
          autoRefreshToken: true,
          persistSession: true,
          detectSessionInUrl: false // Disable auto-detection of auth redirects
        }
      }
    );
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private async clearLocks(): Promise<void> {
    try {
      if ('locks' in navigator) {
        const lockManager = (navigator as any).locks;
        const lockName = 'lock:sb-jwpdgikqwfudfnemtxmt-auth-token';
        
        // Check if the lock exists
        const query = await lockManager.query();
        const hasLock = query.held.some((lock: any) => lock.name === lockName);
        
        if (hasLock) {
          await lockManager.request(
            lockName,
            { mode: 'exclusive', ifAvailable: true },
            async (lock: any) => {
              if (lock) {
                // Lock acquired, now we can release it
                return;
              }
            },
            { timeout: this.lockTimeout }
          );
        }
      }
    } catch (error) {
      console.warn('Error clearing locks:', error);
      // Continue even if lock clearing fails
    }
  }

  private async acquireLock(): Promise<boolean> {
    try {
      if ('locks' in navigator) {
        const lockManager = (navigator as any).locks;
        const lockName = 'lock:sb-jwpdgikqwfudfnemtxmt-auth-token';
        
        // Check if the lock exists
        const query = await lockManager.query();
        const hasLock = query.held.some((lock: any) => lock.name === lockName);
        
        if (hasLock) {
          await lockManager.request(
            lockName,
            { mode: 'exclusive', ifAvailable: true },
            async (lock: any) => {
              if (lock) {
                return true;
              }
              return false;
            },
            { timeout: this.lockTimeout }
          );
        }
      }
      return true;
    } catch (error) {
      console.warn('Error acquiring lock:', error);
      return false;
    }
  }

  register(email: string, password: string): Observable<SupabaseUser> {
    console.log('Attempting to register with Supabase...', { email, passwordLength: password.length });
    return from(this.supabase.auth.signUp({
      email,
      password
    })).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          console.error('Supabase registration error:', response.error);
          throw new Error(response.error.message);
        }
        if (!response.data.user) {
          throw new Error('No user returned from Supabase');
        }
        console.log('Supabase registration successful:', { userFromSupabase: response.data.user });
        console.log('ResponseFromSupabase:', response);
        return response.data.user;
      }),
      catchError(error => {
        console.error('Detailed registration error:', error);
        return throwError(() => error);
      })
    );
  }

  login(email: string, password: string): Observable<RoleType> {
    return from(this.supabase.auth.signInWithPassword({
      email,
      password
    })).pipe(
      takeUntil(this.destroy$),
      switchMap(response => {
        if (response.error) {
          throw new Error(response.error.message);
        }
        if (!response.data.session || !response.data.user) {
          throw new Error('Invalid login response');
        }
        console.log('Login successful:', response.data);
        // Get user details from backend using Supabase ID
        return this.userService.getUserBySupabaseId(response.data.user.id).pipe(
          map(userDetails => {
            // Check if account is disabled
            if (!userDetails.isEnabled) {
              throw new Error('Your account is currently disabled. Please contact support.');
            }
            
            // Set auth state with session and user details
            this.authState.setAuthState(response.data.session!, userDetails);
            
            // Return the role for navigation
            return userDetails.role;
          })
        );
      }),
      catchError(error => throwError(() => error))
    );
  }

  logout(): Observable<void> {
    console.log('SupabaseAuthService: Starting logout process');
    
    // CRITICAL: The block flags should already be set by the ProfileAvatarComponent
    // Just make sure they're definitely set
    window.forceBlockAllRequests = true;
    console.log('SupabaseAuthService: Global HTTP request blocking activated');
    
    // Clear any locks first as an atomic operation
    return from(this.clearLocks()).pipe(
      switchMap(() => {
        console.log('SupabaseAuthService: Locks cleared, proceeding with sign out');
        
        // Sign out from Supabase (this is an async operation)
        return from(this.supabase.auth.signOut()).pipe(
          tap(() => {
            // Make sure auth state is updated AFTER the blocking is in place
            if (this.authState) {
              this.authState.clearAuthState();
            }
            
            console.log('SupabaseAuthService: Signed out from Supabase');
            
            // Clear all storage items that might be related to Supabase
            this.clearStorageOnLogout();
          }),
          // Map the result to void to standardize the Observable type
          map(() => void 0),
          // Handle errors from sign out process
          catchError(error => {
            console.error('SupabaseAuthService: Error during sign out:', error);
            
            // Even on error, we want to clear state and storage
            if (this.authState) {
              this.authState.clearAuthState();
            }
            this.clearStorageOnLogout();
            
            // Propagate the error
            return throwError(() => error);
          }),
          // Make sure we clean up the block flag after a delay
          finalize(() => {
            console.log('SupabaseAuthService: Finalizing logout process');
            
            // IMPORTANT: We no longer automatically clear the forceBlockAllRequests flag here
            // That will be handled by the ProfileAvatarComponent after navigation completes
            // This prevents new HTTP requests from being made during the navigation
          })
        );
      })
    );
  }
  
  /**
   * Clear all storage related to authentication
   */
  private clearStorageOnLogout() {
    console.log('SupabaseAuthService: Clearing storage on logout...');
    
    // Clear specific Supabase-related items from localStorage
    localStorage.removeItem('supabase.auth.token');
    localStorage.removeItem('supabase.auth.expires_at');
    localStorage.removeItem('authSession');
    localStorage.removeItem('supabase-auth-token');
    
    // More thorough cleanup - clear all potential auth-related data
    const keysToRemove: string[] = [];
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && (
        key.includes('supabase') || 
        key.includes('auth') || 
        key.includes('session') ||
        key.includes('user') ||
        key.includes('token')
      )) {
        keysToRemove.push(key);
      }
    }
    
    // Remove all identified keys
    keysToRemove.forEach(key => localStorage.removeItem(key));
    
    // Try to close/cleanup any Supabase-related IndexedDB connections
    console.log('SupabaseAuthService: Allowing browser to clean up unused IndexedDB databases');
    
    // Clear all session storage
    sessionStorage.clear();
    
    // Make an additional check after a delay
    setTimeout(() => {
      console.log('SupabaseAuthService: Running second storage cleanup check...');
      localStorage.removeItem('supabase.auth.token');
      localStorage.removeItem('supabase.auth.expires_at');
      localStorage.removeItem('authSession');
      localStorage.removeItem('supabase-auth-token');
      sessionStorage.clear();
      
      // Also try to completely clear localStorage as a last resort
      // but only for supabase-related items
      keysToRemove.forEach(key => localStorage.removeItem(key));
    }, 500);
  }

  resetPassword(email: string): Observable<void> {
    return from(this.supabase.auth.resetPasswordForEmail(email, {
      redirectTo: `${window.location.origin}/login/reset-password`
    })).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          throw new Error(response.error.message);
        }
        return;
      }),
      catchError(error => throwError(() => error))
    );
  }

  inviteByEmail(email: string, role: string, organizationId: string, teamId?: string): Observable<void> {
    return from(this.supabase.auth.admin.inviteUserByEmail(email, {
      data: {
        role,
        organization_id: organizationId,
        team_id: teamId
      },
      redirectTo: `${window.location.origin}/signup`
    })).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          throw new Error(response.error.message);
        }
        return;
      }),
      catchError(error => throwError(() => error))
    );
  }

  updatePassword(newPassword: string): Observable<void> {
    return from(this.acquireLock()).pipe(
      switchMap(lockAcquired => {
        if (!lockAcquired) {
          console.warn('Could not acquire lock, proceeding anyway');
        }

        // Get the hash from the URL
        const hash = window.location.hash;
        if (!hash) {
          return throwError(() => new Error('No reset token found in URL'));
        }

        // Extract the access token from the hash
        const params = new URLSearchParams(hash.substring(1));
        const accessToken = params.get('access_token');
        const refreshToken = params.get('refresh_token');

        if (!accessToken) {
          return throwError(() => new Error('No access token found in URL'));
        }

        // Set the session with the tokens from the URL
        return from(this.supabase.auth.setSession({
          access_token: accessToken,
          refresh_token: refreshToken || ''
        })).pipe(
          switchMap(() => {
            // Now update the password
            return from(this.supabase.auth.updateUser({ password: newPassword }));
          }),
          takeUntil(this.destroy$),
          map(response => {
            if (response.error) {
              throw new Error(response.error.message);
            }
            return;
          }),
          catchError(error => throwError(() => error))
        );
      })
    );
  }

  // Helper method to get current user
  getCurrentUser(): Observable<User | null> {
    return from(this.supabase.auth.getUser()).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          throw new Error(response.error.message);
        }
        return response.data.user;
      }),
      catchError(error => throwError(() => error))
    );
  }

  // Helper method to get session
  getSession() {
    return this.supabase.auth.getSession();
  }

  // Helper method to set session
  setSession(session: { access_token: string; refresh_token: string }): Observable<void> {
    return from(this.acquireLock()).pipe(
      switchMap(lockAcquired => {
        if (!lockAcquired) {
          console.warn('Could not acquire lock, proceeding anyway');
        }
        
        return from(this.supabase.auth.setSession(session)).pipe(
          takeUntil(this.destroy$),
          map(response => {
            if (response.error) {
              throw new Error(response.error.message);
            }
            return;
          }),
          catchError(error => throwError(() => error))
        );
      })
    );
  }

  // Method to re-authenticate user with current credentials
  reauthenticate(email: string, password: string): Observable<void> {
    return from(this.supabase.auth.signInWithPassword({
      email,
      password
    })).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          throw new Error('Current password is incorrect');
        }
        return;
      }),
      catchError(error => throwError(() => error))
    );
  }

  // Method to update password for already authenticated user
  updateUserPassword(newPassword: string): Observable<void> {
    return from(this.supabase.auth.updateUser({ 
      password: newPassword 
    })).pipe(
      takeUntil(this.destroy$),
      map(response => {
        if (response.error) {
          throw new Error(response.error.message || 'Failed to update password');
        }
        return;
      }),
      catchError(error => throwError(() => error))
    );
  }

  signUp(email: string, password: string): Observable<{ user: any, error: any }> {
    return from(this.supabase.auth.signUp({ email, password })).pipe(
      map(result => ({ user: result.data.user, error: result.error })),
      catchError(error => of({ user: null, error }))
    );
  }
}
