import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { User, Gender, RoleType, UserDetails, UpdateUserCommand, UserDetailsWithRole } from '../models/user.interface';
import { OrganizationService } from './organization.service'; // OrganizationService to get org details
import { Router } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';
import { catchError, tap, map } from 'rxjs/operators';
import { AuthStateService } from '../services/auth-state.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private apiUrl = `${environment.apiUrl}/api/users`;

  private users: User[] = [
  {
      id: '1',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@example.com',
      position: 'System Administrator',
      address: '123 Main St, Springfield, USA',
      dateOfBirth: '1985-06-15',
      profilePictureUrl: 'https://i.pravatar.cc/150?img=1',
      isNotificationEnabled: true,
      isEnabled: true,
      gender: Gender.Male,
      /* organizationId: '1', */ // Only store the organizationId here
      role: RoleType.SuperAdmin,
      createdDate: '2025-01-01'
    },
    {
      id: '2',
      firstName: 'Alice',
      lastName: 'Brown',
      position: 'System Administrator', 
      email: 'alice.brown@example.com',
      address: '456 Oak Rd, London, UK',
      dateOfBirth: '1990-11-23',
      profilePictureUrl: 'https://i.pravatar.cc/150?img=2',
      isNotificationEnabled: true,
      isEnabled: true,
      gender: Gender.Female,
      organizationId: '1',
      role: RoleType.OrganizationAdmin,
      createdDate: '2025-02-01'
    },
    {
      id: '3',
      firstName: 'Michael',
      lastName: 'Smith',
      position: 'Business Analyst',
      email: 'michael.smith@example.com',
      address: '789 Pine Ave, Toronto, Canada',
      dateOfBirth: '1980-08-02',
      profilePictureUrl: 'https://i.pravatar.cc/150?img=3',
      isNotificationEnabled: false,
      isEnabled: true,
      gender: Gender.Male,
      organizationId: '1',
      role: RoleType.TeamManager,
      createdDate: '2025-03-01'
    },
    {
        id: '4',
        firstName: 'Eve',
        lastName: 'Martinez',
        position: 'Software Engineer',
        email: 'eve.martinez@example.com',
        address: '101 Maple St, New York, USA',
        dateOfBirth: '1995-01-10',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=4',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Female,
        organizationId: '1',
        role: RoleType.Collaborator,
        createdDate: '2025-04-01'
      },
      {
        id: '5',
        firstName: 'Charlie',
        lastName: 'Davis',
        position: 'UI/UX Designer',
        email: 'charlie.davis@example.com',
        address: '123 Birch Rd, Berlin, Germany',
        dateOfBirth: '1992-03-15',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=5',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Male,
        organizationId: '1',
        role: RoleType.Collaborator,
        createdDate: '2025-04-02'
      },
      {
        id: '6',
        firstName: 'Sophia',
        lastName: 'Taylor',
        position: 'UI/UX Designer',
        email: 'sophia.taylor@example.com',
        address: '456 Cedar St, Sydney, Australia',
        dateOfBirth: '1993-07-25',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=6',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Female,
        organizationId: '1',
        role: RoleType.Collaborator,
        createdDate: '2025-04-03'
      },
      {
        id: '7',
        firstName: 'David',
        lastName: 'Wilson',
        position: 'Software Engineer',
        email: 'david.wilson@example.com',
        address: '789 Oak Ave, Paris, France',
        dateOfBirth: '1994-09-05',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=7',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Male,
        organizationId: '1',
        role: RoleType.Collaborator,
        createdDate: '2025-04-04'
      },
      {
        id: '8',
        firstName: 'Olivia',
        lastName: 'Johnson',
        position: 'Frontend Developer',
        email: 'olivia.johnson@example.com',
        address: '101 Elm St, Tokyo, Japan',
        dateOfBirth: '1996-11-12',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=8',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Female,
        organizationId: '1',
        role: RoleType.Collaborator,
        createdDate: '2025-04-05'
      },
  
      // New Team Managers
      {
        id: 'cc014b98-90e1-413c-854c-e51b6de94757',
        firstName: 'James',
        lastName: 'Brown',
        position: 'Business Analyst',
        email: 'james.brown@example.com',
        address: '202 Oak St, San Francisco, USA',
        dateOfBirth: '1988-03-22',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=9',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Male,
        organizationId: '1',
        role: RoleType.TeamManager,
        createdDate: '2025-04-06'
      },
      {
        id: '10',
        firstName: 'Isabella',
        lastName: 'Green',
        position: 'Business Analyst',
        email: 'isabella.green@example.com',
        address: '303 Pine St, London, UK',
        dateOfBirth: '1989-06-30',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=10',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Female,
        organizationId: '1',
        role: RoleType.TeamManager,
        createdDate: '2025-04-07'
      },
      {
        id: '11',
        firstName: 'William',
        lastName: 'Lee',
        position: 'Business Analyst',
        email: 'william.lee@example.com',
        address: '404 Maple Rd, Berlin, Germany',
        dateOfBirth: '1987-10-01',
        profilePictureUrl: 'https://i.pravatar.cc/150?img=11',
        isNotificationEnabled: true,
        isEnabled: true,
        gender: Gender.Male,
        organizationId: '1',
        role: RoleType.TeamManager,
        createdDate: '2025-04-08'
      }
  ];

  private isLoggedInSubject = new BehaviorSubject<boolean>(true); // For demo, default to true
  isLoggedIn$ = this.isLoggedInSubject.asObservable();

  constructor(
    private organizationService: OrganizationService, 
    private router: Router,
    private http: HttpClient,
    private authStateService: AuthStateService
  ) {}

  // Get all users
  getUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}`).pipe(
      tap(response => {
        console.log('Raw users API response:', response);
      }),
      catchError(error => {
        console.error('Error fetching users from API:', error);
        return throwError(() => error);
      })
    );
  }

  // Get user by ID
  getUserById(id: string): Observable<User | undefined> {
    // Check if we're in the process of logging out
    if (this.isLoggingOut()) {
      console.log('Skipping getUserById call during logout:', id);
      return of(undefined);
    }
    
    return this.http.get<User>(`${this.apiUrl}/${id}`).pipe(
      catchError(error => {
        console.error('Error fetching user:', error);
        return of(undefined);
      })
    );
  }

  getUserBySupabaseId(supabaseId: string): Observable<UserDetailsWithRole> {
    // Check if we're in the process of logging out
    if (this.isLoggingOut()) {
      console.log('Skipping getUserBySupabaseId call during logout:', supabaseId);
      return throwError(() => new Error('Application is logging out'));
    }
    
    return this.http.get<UserDetailsWithRole>(`${this.apiUrl}/supabase/${supabaseId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching user by Supabase ID:', error);
          return throwError(() => new Error('Failed to fetch user data'));
        })
      );
  }
  
  getUserByIdMock(id: string): Observable<User | undefined> {
    return of(this.users.find(user => user.id === id));
  }

  // Add new method to fetch from backend
  getUserByIdFromApi(id: string): Observable<UserDetails> {
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    });
    console.log('Headers:', headers);

    return this.http.get<UserDetails>(`${this.apiUrl}/${id}`, { headers })
      .pipe(
        tap(user => console.log('Retrieved user details from API:', { ...user, id: '***' })),
        catchError(error => {
          console.error('An error occurred:', error);
          throw error;
        })
      );
  }

  // Create a new user
  createUser(user: Omit<User, 'id'>): Observable<User> {
    const newUser: User = {
      ...user,
      id: (this.users.length + 1).toString(),
      createdDate: new Date().toISOString()
    };
    this.users.push(newUser);
    return of(newUser);
  }

  updateUser(userId: string, userData: UpdateUserCommand): Observable<User> {
    console.log('Updating user with ID:', userId); // Add logging
    if (!userId) {
      console.error('No user ID provided for update');
      return throwError(() => new Error('No user ID provided'));
    }
    return this.http.put<User>(`${this.apiUrl}/${userId}`, userData);
  }

  // Update an existing user
  /* updateUser(id: string, updates: Partial<User>): Observable<User | undefined> {
    const index = this.users.findIndex(user => user.id === id);
    if (index !== -1) {
      this.users[index] = { ...this.users[index], ...updates };
      return of(this.users[index]);
    }
    return of(undefined);
  } */

  // Toggle the user status (enabled/disabled)
  toggleUserStatus(id: string): Observable<User | undefined> {
    const user = this.users.find(user => user.id === id);
    if (user) {
      user.isEnabled = !user.isEnabled;
      return of(user);
    }
    return of(undefined);
  }

  // Delete a user
  /* deleteUser(id: string): Observable<boolean> {
    const index = this.users.findIndex(user => user.id === id);
    if (index !== -1) {
      this.users.splice(index, 1);
      return of(true);
    }
    return of(false);
  } */

  disableUserById(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/disable`, null).pipe(
      tap(() => console.log(`User with ID ${id} disabled successfully.`)),
      catchError(error => {
        console.error(`Error disabling user with ID ${id}:`, error);
        return throwError(() => error);
      })
    );
  }


  enableUserById(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/enable`, null).pipe(
      tap(() => console.log(`User with ID ${id} enabled successfully.`)),
      catchError(error => {
        console.error(`Error enabling user with ID ${id}:`, error);
        return throwError(() => error);
      })
    );
  }

  // Get users by organization ID
  getUsersByOrganizationId(organizationId: string): Observable<UserDetailsWithRole[]> {
    // Check if we're in the process of logging out
    if (this.isLoggingOut()) {
      console.log('Skipping getUsersByOrganizationId call during logout:', organizationId);
      return of([]);
    }
    
    return this.http.get<UserDetailsWithRole[]>(`${this.apiUrl}/organization/${organizationId}`);
  }

  // Get organization details by user (optional helper function)
  getOrganizationDetailsByUser(userId: string): Observable<any> {
    const user = this.users.find(u => u.id === userId);
    if (user && user.organizationId !== undefined) {
      return this.organizationService.getOrganizationById(user.organizationId);
    }
    return of(null); // Return null if user is not found
  }

  // Add this method to the UserService class
  getUsersByIds(userIds: string[]): Observable<User[]> {
    const filteredUsers = this.users.filter(user => userIds.includes(user.id));
    return of(filteredUsers);
  }

  // Add these methods to the UserService class

  verifyPassword(userId: string, currentPassword: string): Observable<boolean> {
    // In a real app, this would be an API call to verify the password
    // For demo purposes, we'll just check if the password is "password123"
    return of(currentPassword === 'password123');
  }

  updatePassword(userId: string, newPassword: string): Observable<boolean> {
    const user = this.users.find(u => u.id === userId);
    if (user) {
      // In a real app, this would be an API call to update the password
      // For demo, we'll just return success
      return of(true);
    }
    return of(false);
  }

  logout() {
    // Clear any stored user data/tokens
    localStorage.removeItem('user');
    localStorage.removeItem('token');
    
    // Update logged in status
    this.isLoggedInSubject.next(false);

    // Redirect to login page
    this.router.navigate(['/login']);
  }

  getTeamManagersByOrganizationId(organizationId: string): Observable<UserDetails[]> {

    return this.http.get<UserDetails[]>(`${this.apiUrl}/organization/${organizationId}/teammanagers`)
      .pipe(
        tap(teamManagers => console.log('Retrieved team managers:', teamManagers)),
        catchError(error => {
          console.error('An error occurred while fetching team managers:', error);
          return throwError(() => new Error('Error fetching team managers.'));
        })
      );
  }
  // Add this method to the UserService class
  // getTeamManagers(): Observable<User[]> {
  //   const teamManagers = this.users.filter(user => user.role === RoleType.TeamManager);
  //   return of(teamManagers);
  // }

  // New method to invite users through the backend API
  inviteByEmail(email: string, role: RoleType, organizationId: string, teamId?: string): Observable<any> {
    const roleNumeric = this.convertRoleToNumeric(role);
    
    return this.http.post<any>(`${this.apiUrl}/invite`, {
      email,
      role: roleNumeric,
      organizationId,
      teamId
    }).pipe(
      tap(response => console.log('Invitation sent successfully:', { email, role: roleNumeric, organizationId })),
      catchError(error => {
        console.error('Error sending invitation:', error);
        return throwError(() => error);
      })
    );
  }

  // Helper method to convert RoleType enum to numeric value
  private convertRoleToNumeric(role: RoleType): number {
    switch (role) {
      case RoleType.SuperAdmin:
        return 0;
      case RoleType.OrganizationAdmin:
        return 1;
      case RoleType.TeamManager:
        return 2;
      case RoleType.Collaborator:
        return 3;
      default:
        console.warn('Unknown role type, defaulting to Collaborator (3)');
        return 3;
    }
  }

  private isLoggingOut(): boolean {
    // Get the AuthStateService via injection in the constructor
    // and check the isLoggingOut flag
    try {
      return this.authStateService.isLoggingOut();
    } catch (e) {
      // If we can't get the auth state service, we'll assume we're not logging out
      return false;
    }
  }

  /**
   * Fetch the email address of the organization admin by organizationId
   * @param organizationId The ID of the organization (GUID)
   * @returns Observable<string> with the admin email
   */
  getOrganizationAdminEmail(organizationId: string): Observable<string> {
    return this.http.get<string>(`${this.apiUrl}/organization/${organizationId}/admin/email`, { responseType: 'text' as 'json' }).pipe(
      tap(email => console.log('Fetched organization admin email:', email)),
      catchError(error => {
        console.error('Error fetching organization admin email:', error);
        return throwError(() => error);
      })
    );
  }

}
