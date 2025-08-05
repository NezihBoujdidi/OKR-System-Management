import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { KeyResult } from '../models/key-result.interface';
import { environment } from '../../environments/environment';
import { Status } from '../models/Status.enum';

@Injectable({
  providedIn: 'root'
})
export class KeyResultService {
  private apiUrl = `${environment.apiUrl}/api/keyresults`;

  constructor(
    private http: HttpClient,
  ) {}

  getKeyResults(): Observable<KeyResult[]> {
    return this.http.get<KeyResult[]>(this.apiUrl)
      .pipe(
        map(results => results.map(result => this.convertDates(result))),
        catchError(error => {
          console.error('Error fetching key results:', error);
          return of([]);
        })
      );
  }

  getKeyResultById(id: string): Observable<KeyResult | undefined> {
    return this.http.get<KeyResult>(`${this.apiUrl}/${id}`)
            .pipe(
        map(result => this.convertDates(result)),
        catchError(error => {
          console.error(`Error fetching key result with ID ${id}:`, error);
          return of(undefined);
      })
    );
  }

  // Helper method to convert status string or enum to numeric value
  private convertStatusToEnum(statusValue: Status | string | undefined): number {
    if (statusValue === undefined) return Status.NotStarted;
    
    if (typeof statusValue === 'number') {
      return statusValue;
    }
    
    switch (statusValue) {
      case 'NotStarted': return Status.NotStarted;
      case 'InProgress': return Status.InProgress;
      case 'Completed': return Status.Completed;
      case 'Overdue': return Status.Overdue;
      default: return Status.NotStarted;
    }
  }

  // Helper method to format date to UTC
  private formatDateToUTC(date: Date | string | undefined): string | undefined {
    if (!date) return undefined;
    
    const d = typeof date === 'string' ? new Date(date) : date;
    return d.toISOString();
  }

  // Add this helper function to convert string dates back to Date objects
  private convertDates(keyResult: any): KeyResult {
    // Create a copy
    const result = { ...keyResult };
    
    // Convert startDate and endDate strings to Date objects if they exist
    if (result.startDate && typeof result.startDate === 'string') {
      result.startDate = new Date(result.startDate);
    }
    
    if (result.endDate && typeof result.endDate === 'string') {
      result.endDate = new Date(result.endDate);
    }
    
    return result as KeyResult;
  }

  createKeyResult(keyResult: Partial<KeyResult> & { objectiveId: string, userId: string }): Observable<KeyResult> {
    // Create a serializable copy with correct enum values and UTC dates
    const keyResultToSend = {
      ...keyResult,
      // Convert status to enum numeric value if present
      status: this.convertStatusToEnum(keyResult.status as any),
      // Format dates to UTC
      startDate: this.formatDateToUTC(keyResult.startDate),
      endDate: this.formatDateToUTC(keyResult.endDate),
      // Ensure objectiveId and userId are included
      objectiveId: keyResult.objectiveId,
      userId: keyResult.userId
    };
    
    console.log('Creating key result with data:', JSON.stringify(keyResultToSend, null, 2));
    
    return this.http.post<KeyResult>(this.apiUrl, keyResultToSend)
      .pipe(
        map(response => this.convertDates(response)), // Convert dates in successful response
        catchError(error => {
          // Handle 200/204 responses that might be treated as errors
          if (error.status === 200 || error.status === 204) {
            console.log('Key result created successfully (status code handled):', error);
            
            // For 204 No Content responses, we need to construct a response
            // using the data we sent, since the server won't return the entity
            if (error.status === 204) {
              const result = {
                ...keyResultToSend,
                id: error.headers?.get('location')?.split('/').pop() || 'temp-id'
              };
              return of(this.convertDates(result));
            }
            
            // For 200 responses, try to parse the response body
            try {
              const result = error.error.text ? JSON.parse(error.error.text) : keyResultToSend;
              return of(this.convertDates(result));
            } catch (e) {
              return of(this.convertDates(keyResultToSend));
            }
          }
          
          console.error('Error creating key result:', error);
          throw error;
        })
      );
  }

  updateKeyResult(id: string, updates: Partial<KeyResult>): Observable<KeyResult | undefined> {
    // Create a serializable copy with proper format
    const updatesToSend = { 
      ...updates,
      // Convert status to enum numeric value if it exists
      ...(updates.status !== undefined && { 
        status: this.convertStatusToEnum(updates.status as any) 
      }),
      // Format dates to UTC if they exist
      ...(updates.startDate && { startDate: this.formatDateToUTC(updates.startDate) }),
      ...(updates.endDate && { endDate: this.formatDateToUTC(updates.endDate) }),
      // Ensure objectiveId and userId are preserved if they exist in updates
      ...(updates.objectiveId && { objectiveId: updates.objectiveId }),
      ...(updates.userId && { userId: updates.userId })
    };
    
    console.log('Updating key result with data:', JSON.stringify(updatesToSend, null, 2));
    
    return this.http.put<KeyResult>(`${this.apiUrl}/${id}`, updatesToSend)
      .pipe(
        catchError(error => {
          // Handle 200/204 responses that might be treated as errors
          if (error.status === 200 || error.status === 204) {
            console.log('Key result updated successfully (status code handled):', error);
            return this.getKeyResultById(id);
          }
          
          console.error(`Error updating key result with ID ${id}:`, error);
    return of(undefined);
        })
      );
  }

  deleteKeyResult(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`)
      .pipe(
        map(() => true),
        catchError(error => {
          // Handle 200/204 responses that might be treated as errors
          if (error.status === 200 || error.status === 204) {
            console.log('Key result deleted successfully (status code handled):', error);
      return of(true);
    }
          
          console.error(`Error deleting key result with ID ${id}:`, error);
    return of(false);
        })
      );
  }

  getKeyResultsByObjectiveId(objectiveId: string): Observable<KeyResult[]> {
    console.log(`Fetching key results for objective ID: ${objectiveId}`);
    console.log('Entered the service keyresults by objectiveId');
    return this.http.get<KeyResult[]>(`${this.apiUrl}/objective/${objectiveId}`)
      .pipe(
        map(results => results.map(result => this.convertDates(result),
        console.log("[KeyResultService] Key results fetched:", results)
      )),
        
        catchError(error => {
          console.error(`Error fetching key results for objective ${objectiveId}:`, error);
          return of([]);
        }
      )
    );
  }
}
