import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse, HttpContext, HttpContextToken } from '@angular/common/http';
import { Observable, throwError, BehaviorSubject, from, EMPTY, Subject } from 'rxjs';
import { catchError, filter, take, switchMap, map, takeUntil } from 'rxjs/operators';
import { AuthStateService } from '../services/auth-state.service';
import { SupabaseAuthService } from '../services/supabase-auth.service';
import { Session } from '@supabase/supabase-js';

// Create a context token for the abort signal
const ABORT_SIGNAL = new HttpContextToken<AbortSignal>(() => new AbortController().signal);

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshTokenSubject: BehaviorSubject<any> = new BehaviorSubject<any>(null);
  
  // Add a flag to force block all requests immediately during logout
  public forceBlockAllRequests = false;
  
  // Track active requests to cancel them during logout
  private abortController = new AbortController();
  private logoutSignal$ = new Subject<void>();

  constructor(
    private authState: AuthStateService,
    private supabaseAuth: SupabaseAuthService
  ) {
    // Subscribe to the loggingOut$ observable to cancel in-flight requests
    this.authState.loggingOut$.subscribe(isLoggingOut => {
      if (isLoggingOut) {
        console.log('AuthInterceptor: Detected logout, aborting in-flight requests');
        // Abort all in-flight requests
        this.abortController.abort();
        // Create a new controller for future requests
        this.abortController = new AbortController();
        // Emit on the logout signal to complete takeUntil streams
        this.logoutSignal$.next();
      }
    });
  }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // First check for force block - both local and global flags
    const globalBlockActive = window.forceBlockAllRequests === true;
    const interceptorBlockActive = this.forceBlockAllRequests === true;
    const logoutInProgress = this.authState.isLoggingOut();
    
    if (globalBlockActive || interceptorBlockActive) {
      console.log(`Request completely BLOCKED during logout: ${request.method} ${request.url}`, {
        globalBlock: globalBlockActive,
        interceptorBlock: interceptorBlockActive,
        logoutInProgress: logoutInProgress
      });
      return EMPTY; // Return empty observable to completely cancel the request
    }
    
    // Skip token operations if we're in the process of logging out
    if (logoutInProgress) {
      console.log(`Request BLOCKED during logout process: ${request.method} ${request.url}`);
      return EMPTY; // Changed from returning next.handle(request) to EMPTY
    }
    
    // Check if the document has the logout-in-progress class
    const logoutClassActive = typeof document !== 'undefined' && 
                             document.body.classList.contains('logout-in-progress');
    if (logoutClassActive) {
      console.log(`Request BLOCKED due to logout class: ${request.method} ${request.url}`);
      return EMPTY;
    }
    
    // Add the AbortSignal to the request using a proper context
    const context = new HttpContext();
    context.set(ABORT_SIGNAL, this.abortController.signal);
    
    request = request.clone({
      context
    });
        
    // Get the current session
    return this.authState.getSession$().pipe(
      map(session => {
        if (session?.access_token) {
          request = this.addToken(request, session.access_token);
        }
        return request;
      }),
      switchMap(modifiedRequest => {
        // One final check before passing the request through
        if (window.forceBlockAllRequests || this.forceBlockAllRequests || this.authState.isLoggingOut()) {
          console.log(`Last-minute BLOCK of request: ${request.method} ${request.url}`);
          return EMPTY;
        }
        // Use takeUntil to automatically cancel this request if logout happens
        return next.handle(modifiedRequest).pipe(
          takeUntil(this.logoutSignal$)
        );
      }),
      catchError(error => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          return this.handle401Error(request, next);
        }
        return throwError(() => error);
      })
    );
  }

  private addToken(request: HttpRequest<any>, token: string) {
    return request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  private handle401Error(request: HttpRequest<any>, next: HttpHandler) {
    if (!this.isRefreshing) {
      this.isRefreshing = true;
      this.refreshTokenSubject.next(null);

      // Get refresh token from session
      return this.authState.getSession$().pipe(
        take(1),
        switchMap(session => {
          if (!session?.refresh_token) {
            this.isRefreshing = false;
            this.supabaseAuth.logout().subscribe();
            return throwError(() => new Error('No refresh token available'));
          }

          // Use Supabase to refresh the session
          return from(this.supabaseAuth.getSession()).pipe(
            switchMap((response: { data: { session: Session | null }, error: any }) => {
              if (response.error) {
                throw new Error(response.error.message);
              }

              this.isRefreshing = false;
              const newSession = response.data.session;
              if (newSession) {
                this.refreshTokenSubject.next(newSession.access_token);
                return next.handle(this.addToken(request, newSession.access_token));
              }
              throw new Error('Failed to refresh session');
            }),
            catchError(error => {
              this.isRefreshing = false;
              this.supabaseAuth.logout().subscribe();
              return throwError(() => error);
            })
          );
        })
      );
    }

    return this.refreshTokenSubject.pipe(
      filter(token => token != null),
      take(1),
      switchMap(token => {
        return next.handle(this.addToken(request, token));
      })
    );
  }
}