import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { Objective } from '../models/objective.interface';
import { Priority } from '../models/Priority.enum';
import { Status } from '../models/Status.enum';
import { UserService } from './user.service';
import { TeamService } from './team.service';
import { User } from '../models/user.interface';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ObjectiveService {
  private apiUrl = `${environment.apiUrl}/api/objectives`;
  
  // Cache for team managers to prevent repeated API calls
  private teamManagerCache: Map<string, User | undefined> = new Map();

  constructor(
    private http: HttpClient,
    private userService: UserService,
    private teamService: TeamService
  ) {}

  getObjectives(): Observable<Objective[]> {
    return this.http.get<Objective[]>(this.apiUrl)
      .pipe(
        catchError(error => {
          console.error('Error fetching objectives:', error);
          return of([]);
        })
      );
  }

  getObjectiveById(id: string): Observable<Objective | undefined> {
    return this.http.get<Objective>(`${this.apiUrl}/${id}`)
      .pipe(
        catchError(error => {
          console.error(`Error fetching objective with ID ${id}:`, error);
          return of(undefined);
        })
      );
  }

  getObjectiveOwner(userId: string): Observable<User | undefined> {
    console.log('Fetching objective owner details for userId:', userId);
    return this.userService.getUserById(userId);
  }

  getObjectiveManager(teamId: string): Observable<User | undefined> {
    // Check cache first
    if (this.teamManagerCache.has(teamId)) {
      console.log('Returning cached manager for teamId:', teamId);
      return of(this.teamManagerCache.get(teamId));
    }
    
    console.log('Fetching objective manager details for teamId:', teamId);
    return this.teamService.getTeamManagerDetails(teamId).pipe(
      tap(manager => {
        // Cache the result, even if it's undefined
        this.teamManagerCache.set(teamId, manager);
      })
    );
  }

  getObjectivesBySessionId(sessionId: string): Observable<Objective[]> {
    return this.http.get<Objective[]>(`${this.apiUrl}/session/${sessionId}`)
      .pipe(
        catchError(error => {
          console.error(`Error fetching objectives for session ${sessionId}:`, error);
          return of([]);
        })
      );
  }

  // Helper method to convert priority string to enum value
  private convertPriorityToEnum(priorityString: string | undefined): number {
    if (!priorityString) return Priority.Medium;
    
    switch (priorityString) {
      case 'Low': return Priority.Low;
      case 'Medium': return Priority.Medium;
      case 'High': return Priority.High;
      case 'Urgent': return Priority.Urgent;
      default: return Priority.Medium;
    }
  }

  // Helper method to convert status string to enum value
  private convertStatusToEnum(statusString: string | undefined): number {
    if (!statusString) return Status.NotStarted;
    
    switch (statusString) {
      case 'NotStarted': return Status.NotStarted;
      case 'InProgress': return Status.InProgress;
      case 'Completed': return Status.Completed;
      case 'Overdue': return Status.Overdue;
      default: return Status.NotStarted;
    }
  }

  createObjective(objective: Omit<Objective, 'id'>): Observable<Objective> {
    // Create a serializable copy with correct enum values
    const objectiveToSend = {
      ...objective,
      // Convert string values to enum numeric values
      priority: this.convertPriorityToEnum(objective.priority as unknown as string),
      status: this.convertStatusToEnum(objective.status as unknown as string)
    };

    console.log('Creating objective with data:', JSON.stringify(objectiveToSend, null, 2));
    
    return this.http.post<Objective>(this.apiUrl, objectiveToSend)
      .pipe(
        catchError(error => {
          console.error('Error creating objective:', error);
          throw error;
        })
      );
  }

  updateObjective(id: string, updates: Partial<Objective>): Observable<Objective | undefined> {
    // Create a serializable copy and ensure enums are properly converted
    const updatesToSend = { 
      ...updates,
      // Convert string priority to enum numeric value if it exists
      ...(updates.priority !== undefined && { 
        priority: this.convertPriorityToEnum(updates.priority as unknown as string) 
      }),
      // Convert string status to enum numeric value if it exists
      ...(updates.status !== undefined && { 
        status: this.convertStatusToEnum(updates.status as unknown as string) 
      })
    };

    console.log('Updating objective with data:', JSON.stringify(updatesToSend, null, 2));
    
    return this.http.put<Objective>(`${this.apiUrl}/${id}`, updatesToSend)
      .pipe(
        catchError(error => {
          console.error(`Error updating objective with ID ${id}:`, error);
          return of(undefined);
        })
      );
  }

  deleteObjective(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`)
      .pipe(
        map(() => true),
        catchError(error => {
          console.error(`Error deleting objective with ID ${id}:`, error);
          return of(false);
        })
      );
  }

  clearManagerCache() {
    this.teamManagerCache.clear();
  }
}
