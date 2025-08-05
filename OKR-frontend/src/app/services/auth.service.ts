import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Observable, throwError, switchMap, map } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { 
  RegisterUserCommand, 
  LoginUserCommand, 
  ForgotPasswordCommand,
  ResetPasswordCommand,
  ConfirmEmailCommand,
  RefreshTokenCommand,
  LoginResponse,
  AuthResponse,
  GenerateInvitationLinkCommand
} from '../models/auth.interface';
import { UserDetails, UserDetailsWithRole } from '../models/user.interface';
import { environment } from '../../environments/environment';
import { AuthStateService } from './auth-state.service';
import { JwtHelperService } from './jwt-helper.service';
import { UserService } from './user.service';
import { RoleType } from '../models/role-type.enum';

interface ValidateKeyDto {
  token: string;
  expirationDate: Date;
  organizationId: string;
}

interface InviteKeyDetails {
  role: RoleType;
  expirationDate: Date;
  organizationId: string;
  email: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/accounts`;

  constructor(
    private http: HttpClient,
    private authState: AuthStateService,
    private jwtHelper: JwtHelperService,
    private userService: UserService
  ) {}

  //register(command: RegisterUserCommand): Observable<any> {
  register(command: RegisterUserCommand & { supabaseId: string }): Observable<any> {
    console.log('Registering user with command:', command);
    return this.http.post<any>(`${this.apiUrl}/register`, command)
      .pipe(
        tap(response => console.log('Raw registration response:', response)),
        catchError(this.handleError)
      );
  }

  /* login(command: LoginUserCommand): Observable<AuthResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, command)
      .pipe(
        switchMap(loginResponse => {
          console.log('Login Response:', loginResponse);
          
          // Check if the token is present
          if (!loginResponse.token) {
            throw new Error('No token received');
          }

          // Decode the token to inspect its contents
          const decodedToken = this.jwtHelper.decodeToken(loginResponse.token);
          console.log('Decoded Token:', decodedToken);

          const userId = this.jwtHelper.getUserIdFromToken(loginResponse.token);
          const role = this.jwtHelper.getRole(loginResponse.token);
          
          if (!userId || !role) throw new Error('Invalid token');
          
          return this.userService.getUserByIdFromApi(userId).pipe(
            map(userDetails => {
              const authResponse: AuthResponse = {
                ...loginResponse,
                user: { 
                  ...userDetails, 
                  role: role
                } as UserDetailsWithRole
              };
              this.authState.setAuthState(authResponse);
              return authResponse;
            })
          );
        }),
        catchError(this.handleError)
      );
  } */

  generateInvitationLink(command: GenerateInvitationLinkCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/generate-invitation-link`, command).pipe(
      tap(() => console.log(`Invitation link generated for ${command.email}`)),
      catchError(this.handleError)
    );
  }

  inviteByEmail(command: GenerateInvitationLinkCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/invite`, command).pipe(
      tap(() => console.log(`Invitation sent to ${command.email}`)),
      catchError(error => {
        console.error('Error sending invitation:', error);
        return this.handleError(error);
      })
    );
  }

  /* forgotPassword(command: ForgotPasswordCommand): Observable<void> {
    console.log('Sending forgot password request to:', `${this.apiUrl}/forgot-password`);
    return this.http.post<void>(`${this.apiUrl}/forgot-password`, command)
      .pipe(
        tap(() => console.log('Forgot password request successful')),
        catchError(error => {
          console.error('Forgot password error in service:', error);
          return this.handleError(error);
        })
      );
  }

  resetPassword(command: ResetPasswordCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/reset-password`, command)
      .pipe(catchError(this.handleError));
  }

  confirmEmail(command: ConfirmEmailCommand): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/confirm-email`, command)
      .pipe(catchError(this.handleError));
  } */

  /* refreshToken(command: RefreshTokenCommand): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh-token`, command)
      .pipe(
        tap(response => {
          this.authState.setAuthState(response);
        }),
        catchError(this.handleError)
      );
  } */

  /* logout() {
    this.authState.clearAuthState();
  } */

  /* getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem('refreshToken');
  }

  isAuthenticated(): boolean {
    return this.authState.isAuthenticated();
  } */

  validateInviteKey(key: string): Observable<InviteKeyDetails> {
    return this.http.get<ValidateKeyDto>(
      `${this.apiUrl}/validate-invite-key`, 
      { 
        params: { key }
      }
    ).pipe(
      map(response => {
        const role = this.jwtHelper.getRole(response.token);
        const organizationId = this.jwtHelper.getOrganizationId(response.token);
        const email = this.jwtHelper.getEmail(response.token);
        console.log('Organization ID:', organizationId);
        console.log('Email from token:', email);
        
        if (!role || !organizationId || !email) {
          throw new Error('Invalid token: missing role, organizationId, or email');
        }

        return {
          role: role as RoleType,
          expirationDate: new Date(response.expirationDate),
          organizationId: organizationId,
          email: email
        };
      }),
      catchError(this.handleError)
    );
  }

  private handleError(error: HttpErrorResponse) {
    console.log('Auth Service Error:', error); // Log the full error

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      return throwError(() => error.error.message);
    }
    
    // Server-side error
    if (error.status === 400) {
      // Validation errors
      if (error.error?.errors) {
        return throwError(() => error.error.errors);
      }
      // If it's a string message
      if (typeof error.error === 'string') {
        return throwError(() => error.error);
      }
      return throwError(() => error.error?.message || 'Validation failed');
    } else if (error.status === 401) {
      return throwError(() => 'Unauthorized access');
    } else if (error.status === 404) {
      return throwError(() => 'Resource not found');
    } else if (error.status === 500) {
      return throwError(() => 'Server error');
    }

    return throwError(() => 'An unexpected error occurred');
  }
}
