import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { Team, ExtendedTeam, UpdateTeamCommand } from '../models/team.interface';
import { User } from '../models/user.interface';
import { UserService } from './user.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { catchError, map, tap, switchMap } from 'rxjs/operators';
import { PaginatedResult } from '../models/paginatedResult';
import { UserDetailsWithRole } from '../models/user.interface';

@Injectable({
  providedIn: 'root'
})
export class TeamService {
  private apiUrl = `${environment.apiUrl}/api`;
  private teams: Team[] = [
    {
      id: '35778569-ec4a-4978-9ab2-4843bff56588',
      name: 'Engineering Team',
      description: 'Responsible for the product development and maintenance.',
      organizationId: '1', // Link to organization
      teamManagerId: '9', // User ID of the Team Manager
      collaboratorIds: ['4', '5', '6', '7'], // Collaborators in the team
      isActive: true,
      createdDate: new Date('2025-01-01')
    },
    {
      id: '2',
      name: 'Design Team',
      description: 'Focused on the UI/UX design and product visuals.',
      organizationId: '1',
      teamManagerId: '9',
      collaboratorIds: ['4', '8'], // Collaborators in the team
      isActive: true,
      createdDate: new Date('2025-02-01')
    },
    {
      id: '3',
      name: 'Marketing Team',
      description: 'Responsible for marketing campaigns and content strategy.',
      organizationId: '1',
      teamManagerId: '11',
      collaboratorIds: ['5', '6'],
      isActive: true,
      createdDate: new Date('2025-03-01')
    },
    {
      id: '4',
      name: 'Sales Team',
      description: 'In charge of customer relations and increasing sales.',
      organizationId: '1',
      teamManagerId: '3',
      collaboratorIds: ['6', '7'],
      isActive: true,
      createdDate: new Date('2025-04-01')
    },
    {
      id: '5',
      name: 'Customer Support Team',
      description: 'Handles customer inquiries and product support.',
      organizationId: '1',
      teamManagerId: '11',
      collaboratorIds: ['7', '8'],
      isActive: true,
      createdDate: new Date('2025-05-01')
    }
  ];

  constructor(private userService: UserService, private http: HttpClient) {}

  
  getTeams(): Observable<Team[]> {
    return of(this.teams);
  }

  getAllTeams(): Observable<PaginatedResult<Team>> {
    return this.http.get<PaginatedResult<Team>>(`${this.apiUrl}?page=1&pageSize=1000`);
  }

  // Get a team by its ID
  getTeamById(id: string): Observable<Team | undefined> {
    return of(this.teams.find(team => team.id === id));
  }

  // Create a new team
  createTeam(team: { name: string; description?: string; organizationId: string; teamManagerId?: string }): Observable<string> {
    const command = {
      name: team.name,
      description: team.description ?? '',
      organizationId: team.organizationId,
      teamManagerId: team.teamManagerId ?? null
    };

    return this.http.post<string>(`${this.apiUrl}/teams`, command);
  }

  // Update an existing team
  updateTeam(id: string, updates: Partial<Team>): Observable<Team> {
    // Create an UpdateTeamCommand object
    const command: UpdateTeamCommand = {
      name: updates.name || '',
      description: updates.description || '',
      organizationId: updates.organizationId || '',
      teamManagerId: updates.teamManagerId || ''
    };

    // Call the API with the proper URL and command structure
    return this.http.put<Team>(`${this.apiUrl}/teams/${id}`, command, { responseType: 'text' as 'json' })
      .pipe(
        map(() => {
          // Return a partial Team object representing the updated team
          return { id, ...updates } as Team;
        }),
        catchError(error => {
          console.error('Error updating team:', error);
          return throwError(() => new Error('Failed to update team'));
        })
      );
  }

  removeUserFromTeam(teamId: string, userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/teamusers/team/${teamId}/user/${userId}`);
  }

  // Direct method to move a member from one team to another using the backend endpoint
  moveMemberFromTeamToTeam(memberId: string, sourceTeamId: string, newTeamId: string): Observable<void> {
    console.log("Moving user with id :" , memberId);
    return this.http.post<void>(`${this.apiUrl}/teamusers/move-member`, {
      memberId: memberId,
      sourceTeamId: sourceTeamId,
      newTeamId: newTeamId
    });
  }

  // Toggle the team status (active/inactive)
  toggleTeamStatus(id: string): Observable<Team | undefined> {
    const team = this.teams.find(t => t.id === id);
    if (team) {
      team.isActive = !team.isActive;
      return of(team);
    }
    return of(undefined);
  }

  // Delete a team
  deleteTeam(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/teams/${id}`, { responseType: 'text' as 'json' })
      .pipe(
        map(() => undefined), // Transform text response to void
        catchError(error => {
          console.error('Error deleting team:', error);
          return throwError(() => new Error('Failed to delete team'));
        })
      );
  }

  // Get all teams of a specific organization
  getTeamsByOrganizationId(organizationId: string): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/teams/organization/${organizationId}`);
  }

  getUsersByTeamId(teamId: string): Observable<User[]> {
    return this.http.get<User[]>(`${this.apiUrl}/teamusers/team/${teamId}/users`);
  }

  // Get the team manager details for a specific team
  getTeamManagerDetails(teamId: string): Observable<User | any> {
    return this.http.get<User>(`${this.apiUrl}/teams/manager/details/${teamId}`)
      .pipe(
        catchError(error => { 
          console.error('Error fetching team manager details:', error);
          return of(null); 
        }
        ));
  }

  // Get the collaborators for a specific team
  getCollaboratorsForTeam(teamId: string): Observable<UserDetailsWithRole[]> {
    return this.http.get<UserDetailsWithRole[]>(`${this.apiUrl}/teamusers/team/${teamId}/users`)
      .pipe(
        tap(users => console.log(`Retrieved ${users.length} collaborators for team ${teamId}`)),
        catchError(error => {
          console.error('Error fetching team collaborators:', error);
          return throwError(() => new Error('Failed to fetch team collaborators'));
        })
      );
  }

  getTeamsByManagerId(managerId: string): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/teams/manager/${managerId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching teams by manager ID:', error);
          // Fallback to the in-memory array in case of error
          return of([]);
        })
      );
  }

  // Add this new method
  getTeamsByCollaboratorId(collaboratorId: string): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/teams/collaborator/${collaboratorId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching teams by collaborator ID:', error);
          return of([]);
        })
      );
  }

  // Add this new method
  getTeamsByUserId(userId: string): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/teams/by-user/${userId}`)
      .pipe(
        catchError(error => {
          console.error('Error fetching teams by user ID:', error);
          return of([]);
        })
      );
  }

  // Get teams assigned to a specific OKR session
  getTeamsBySessionId(sessionId: string): Observable<Team[]> {
    return this.http.get<Team[]>(`${this.apiUrl}/teams/session/${sessionId}`).pipe(
      catchError(error => {
        console.error('Error fetching teams by session ID:', error);
        return of([]);
      })
    );
  }

  // New method to add users to a team
  addUsersToTeam(teamId: string, userIds: string[]): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/teamusers/add-users`, {
      teamId: teamId,
      userIds: userIds
    }).pipe(
      tap(response => console.log('Add users to team response:', response)),
      catchError(error => {
        console.error('Error adding users to team:', error);
        return throwError(() => error);
      })
    );
  }

  // Get all collaborators in an organization (for showing in the modal)
  getCollaboratorsInOrganization(organizationId: string): Observable<UserDetailsWithRole[]> {
    return this.http.get<UserDetailsWithRole[]>(`${this.apiUrl}/users/organization/${organizationId}/collaborators`).pipe(
      tap(collaborators => console.log(`Retrieved ${collaborators.length} collaborators for organization ${organizationId}`)),
      catchError(error => {
        console.error('Error fetching organization collaborators:', error);
        return throwError(() => error);
      })
    );
  }
}

