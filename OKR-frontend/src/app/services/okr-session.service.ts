import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap, map, mergeMap } from 'rxjs/operators';
import { OKRSession, CreateOkrCommand, UpdateOkrCommand } from '../models/okr-session.interface';
import { Status } from '../models/Status.enum';
import { User } from '../models/user.interface';
import { UserService } from './user.service';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';

// First, let's add an interface for the paginated response
interface PaginatedResponse<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class OKRSessionService {
  private apiUrl = `${environment.apiUrl}/api/okrsessions`;
  private okrSessionsSubject = new BehaviorSubject<OKRSession[]>([]);
  public okrSessions$ = this.okrSessionsSubject.asObservable();
  private activeSessionSubject = new BehaviorSubject<string | null>(null);
  public activeSession$ = this.activeSessionSubject.asObservable();
  private organizationSessionsCache = new Map<string, OKRSession[]>();

  constructor(private userService: UserService, private http: HttpClient) {}

  private handleError(error: HttpErrorResponse) {
    console.log('OKR Service Error:', error);

    if (error.error instanceof ErrorEvent) {
      return throwError(() => error.error.message);
    }
    
    if (error.status === 400) {
      if (error.error?.errors) {
        return throwError(() => error.error.errors);
      }
      if (typeof error.error === 'string') {
        return throwError(() => error.error);
      }
      return throwError(() => error.error?.message || 'Validation failed');
    }
    
    return throwError(() => 'An unexpected error occurred');
  }

  setActiveSession(sessionId: string | null) {
    this.activeSessionSubject.next(sessionId);
  }

  getActiveSession(): Observable<string | null> {
    return this.activeSession$;
  }

  getOKRSessions(): Observable<OKRSession[]> {
    return this.http.get<PaginatedResponse<OKRSession>>(this.apiUrl).pipe(
      map(response => {
        console.log('Raw response from API:', response);
        return response.items.map(session => {
          console.log('Processing session:', {
            id: session.id,
            title: session.title,
            rawStatus: session.status,
            statusType: typeof session.status
          });
          
          let processedStatus: Status;

          if (session.status === null || session.status === undefined) {
            processedStatus = Status.NotStarted;
            console.log('Null/undefined status, defaulting to NotStarted');
          } else if (typeof session.status === 'string') {
            // Map string status to enum values
            switch (session.status) {
              case 'NotStarted':
                processedStatus = Status.NotStarted;
                break;
              case 'InProgress':
                processedStatus = Status.InProgress;
                break;
              case 'Completed':
                processedStatus = Status.Completed;
                break;
              case 'Overdue':
                processedStatus = Status.Overdue;
                break;
              default:
                processedStatus = Status.NotStarted;
                console.log('Unknown status string:', session.status);
            }
            console.log('Mapped string status to:', processedStatus);
          } else if (typeof session.status === 'number') {
            if (Object.values(Status).includes(session.status)) {
              processedStatus = session.status;
            } else {
              processedStatus = Status.NotStarted;
            }
          } else {
            processedStatus = Status.NotStarted;
          }

          const processedSession = {
            ...session,
            status: processedStatus
          };
          
          console.log('Processed session:', {
            id: processedSession.id,
            title: processedSession.title,
            finalStatus: processedSession.status
          });

          return processedSession;
        });
      }),
      tap(sessions => {
        console.log('All processed sessions:', sessions);
        this.okrSessionsSubject.next(sessions);
      }),
      catchError(error => {
        console.error('Error fetching OKR sessions:', error);
        return throwError(() => error);
      })
    );
  }

  getOKRSessionsByOrganizationId(id: string): Observable<OKRSession[]> {
    return this.http.get<OKRSession[]>(`${this.apiUrl}/organization/${id}`).pipe(
      map(sessions => {
        // Process sessions to ensure proper status values
        return sessions.map(session => {
          let processedStatus: Status;
          
          if (session.status === null || session.status === undefined) {
            processedStatus = Status.NotStarted;
          } else if (typeof session.status === 'string') {
            // Map string status to enum values
            switch (session.status) {
              case 'NotStarted': processedStatus = Status.NotStarted; break;
              case 'InProgress': processedStatus = Status.InProgress; break;
              case 'Completed': processedStatus = Status.Completed; break;
              case 'Overdue': processedStatus = Status.Overdue; break;
              default: processedStatus = Status.NotStarted;
            }
          } else if (typeof session.status === 'number') {
            if (Object.values(Status).includes(session.status)) {
              processedStatus = session.status;
            } else {
              processedStatus = Status.NotStarted;
            }
          } else {
            processedStatus = Status.NotStarted;
          }
          
          // Make sure to include the organizationId in each session
          return {
            ...session,
            status: processedStatus,
            organizationId: id
          };
        });
      }),
      tap(sessions => {
        // Cache the sessions for this organization
        this.organizationSessionsCache.set(id, sessions);
        
        // Also update the main sessions list with these sessions
        // First, get current sessions that don't belong to this organization
        const currentSessions = this.okrSessionsSubject.value;
        const otherSessions = currentSessions.filter(s => s.organizationId !== id);
        
        // Then merge with the new organization sessions
        const updatedSessions = [...otherSessions, ...sessions];
        
        // Update the BehaviorSubject
        this.okrSessionsSubject.next(updatedSessions);
      }),
      catchError(error => {
        console.error(`Error fetching sessions for organization ${id}:`, error);
        return throwError(() => error);
      })
    );
  }

  // Add method to get OKR sessions by team ID
  getOKRSessionsByTeamId(teamId: string): Observable<OKRSession[]> {
    return this.http.get<OKRSession[]>(`${this.apiUrl}/by-teamId/${teamId}`).pipe(
      map(sessions => {
        // Process sessions to ensure proper status values
        return sessions.map(session => {
          let processedStatus: Status;
          
          if (session.status === null || session.status === undefined) {
            processedStatus = Status.NotStarted;
          } else if (typeof session.status === 'string') {
            // Map string status to enum values
            switch (session.status) {
              case 'NotStarted': processedStatus = Status.NotStarted; break;
              case 'InProgress': processedStatus = Status.InProgress; break;
              case 'Completed': processedStatus = Status.Completed; break;
              case 'Overdue': processedStatus = Status.Overdue; break;
              default: processedStatus = Status.NotStarted;
            }
          } else if (typeof session.status === 'number') {
            if (Object.values(Status).includes(session.status)) {
              processedStatus = session.status;
            } else {
              processedStatus = Status.NotStarted;
            }
          } else {
            processedStatus = Status.NotStarted;
          }
          
          return {
            ...session,
            status: processedStatus,
            teamId: teamId
          };
        });
      }),
      catchError(error => {
        console.error(`Error fetching sessions for team ${teamId}:`, error);
        return throwError(() => error);
      })
    );
  }

  clearOrganizationSessionsCache(organizationId?: string) {
    if (organizationId) {
      this.organizationSessionsCache.delete(organizationId);
    } else {
      this.organizationSessionsCache.clear();
    }
  }

  getOKRSessionById(id: string): Observable<OKRSession | undefined> {
    return this.http.get<OKRSession>(`${this.apiUrl}/${id}`).pipe(
      catchError(this.handleError)
    );
  }

  // Add a debug method to inspect session data
  debugSessionData(id: string): Observable<any> {
    console.log(`===== DEBUG: Fetching detailed session data for ID: ${id} =====`);
    return this.http.get<OKRSession>(`${this.apiUrl}/${id}`).pipe(
      map(session => {
        console.log('Full Session Data:', JSON.stringify(session, null, 2));
        console.log('Session Teams:', session.teamIds);
        console.log('Session Organization:', session.organizationId);
        return session;
      }),
      catchError(error => {
        console.error('Error fetching session data for debugging:', error);
        throw error;
      })
    );
  }

  createOkrSession(command: CreateOkrCommand): Observable<void> {
    console.log('Creating OKR session:', command);

    const requestBody = {
      Title: command.title,
      Description: command.description || '',
      StartedDate: command.startedDate.toISOString(),
      EndDate: command.endDate.toISOString(),
      TeamIds: command.teamIds || [],
      UserId: command.userId,
      Color: command.color || '#4299E1',
      Status: command.status || Status.NotStarted,
      Priority: command.priority || 1,
      // Include organization ID if provided
      ...(command.organizationId ? { OrganizationId: command.organizationId } : {})
    };

    console.log('Making create request:', {
      url: this.apiUrl,
      body: requestBody
    });

    return this.http.post<void>(this.apiUrl, requestBody).pipe(
      tap(() => {
        // Refresh the sessions list after creation
        this.getOKRSessions().subscribe();
      }),
      catchError(this.handleError)
    );
  }

  updateOKRSession(id: string, command: UpdateOkrCommand): Observable<void> {
    // Get any additional properties from the command object
    const { title, description, startedDate, endDate, teamIds, userId, color, status, ...additionalProps } = command as any;
    
    const requestBody = {
      Title: title,
      Description: description || '',
      StartedDate: startedDate.toISOString(),
      EndDate: endDate.toISOString(),
      TeamIds: teamIds || [],
      UserId: userId,
      Color: color || '#4299E1',
      Status: status,
      // Include organizationId if it was passed in the additionalProps
      ...(additionalProps.organizationId ? { OrganizationId: additionalProps.organizationId } : {})
    };

    console.log('Making update request:', {
      url: `${this.apiUrl}/${id}`,
      body: requestBody
    });
    
    return this.http.put<void>(`${this.apiUrl}/${id}`, requestBody).pipe(
      tap(() => {
        console.log('Update request successful');
        
        // Get the current sessions list
        const currentSessions = this.okrSessionsSubject.value;
        
        // Find the session being updated
        const sessionIndex = currentSessions.findIndex(s => s.id === id);
        
        if (sessionIndex !== -1) {
          // Create updated session object
          const updatedSession = {
            ...currentSessions[sessionIndex],
            title: title,
            description: description || '',
            startedDate: startedDate instanceof Date ? startedDate.toISOString() : startedDate,
            endDate: endDate instanceof Date ? endDate.toISOString() : endDate,
            color: color || '#4299E1',
            status: status
          };
          
          // Create a new array with the updated session
          const updatedSessions = [
            ...currentSessions.slice(0, sessionIndex),
            updatedSession,
            ...currentSessions.slice(sessionIndex + 1)
          ];
          
          // Update the BehaviorSubject
          this.okrSessionsSubject.next(updatedSessions);
        }
        
        // Clear any cached data
        this.clearOrganizationSessionsCache();
      }),
      catchError((error: HttpErrorResponse) => {
        // Check if it's actually a success response
        if (error.status === 200 || error.status === 204) {
          console.log('Update successful despite error response');
          
          // Get the current sessions list
          const currentSessions = this.okrSessionsSubject.value;
          
          // Find the session being updated
          const sessionIndex = currentSessions.findIndex(s => s.id === id);
          
          if (sessionIndex !== -1) {
            // Create updated session object
            const updatedSession = {
              ...currentSessions[sessionIndex],
              title: title,
              description: description || '',
              startedDate: startedDate instanceof Date ? startedDate.toISOString() : startedDate,
              endDate: endDate instanceof Date ? endDate.toISOString() : endDate,
              color: color || '#4299E1',
              status: status
            };
            
            // Create a new array with the updated session
            const updatedSessions = [
              ...currentSessions.slice(0, sessionIndex),
              updatedSession,
              ...currentSessions.slice(sessionIndex + 1)
            ];
            
            // Update the BehaviorSubject
            this.okrSessionsSubject.next(updatedSessions);
          }
          
          // Clear any cached data
          this.clearOrganizationSessionsCache();
          return of(void 0);
        }
        
        console.error('Update request failed:', error);
        return throwError(() => error);
      })
    );
  }

  deleteOKRSession(id: string): Observable<void> {
    console.log(`Deleting OKR session ${id}`);
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap(() => {
        // Update local state immediately
        const currentSessions = this.okrSessionsSubject.value;
        const updatedSessions = currentSessions.filter(session => session.id !== id);
        
        // Important: This updates the BehaviorSubject which will notify all subscribers
        this.okrSessionsSubject.next(updatedSessions);
        
        // Clear any cached data
        this.clearOrganizationSessionsCache();
      }),
      // Handle the response
      map(() => {
        console.log('Session deleted successfully');
        return void 0;
      }),
      catchError(error => {
        // Check if it's a successful delete despite JSON parse error
        if (error.status === 200 || error.status === 204) {
          console.log('Delete successful (status:', error.status, ')');
          
          // Update local state again as a safeguard
          const currentSessions = this.okrSessionsSubject.value;
          const updatedSessions = currentSessions.filter(session => session.id !== id);
          this.okrSessionsSubject.next(updatedSessions);
          
          // Clear any cached data
          this.clearOrganizationSessionsCache();
          return of(void 0);
        }
        
        // Real error case
        console.error('Delete failed:', error);
        return throwError(() => ({
          message: 'Failed to delete session',
          status: error.status,
          error: error
        }));
      })
    );
  }

  // toggleOKRSessionStatus(id: string): Observable<OKRSession | undefined> {
  //   console.log(`Toggling status for OKR session ${id}`);
  //   return this.getOKRSessionById(id).pipe(
  //     tap(session => {
  //       if (session) {
  //         const update = { isActive: !session.isActive };
  //         this.updateOKRSession(id, update).subscribe();
  //       }
  //     }),
  //     catchError(this.handleError)
  //   );
  // }
}