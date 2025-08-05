import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { KeyResultTask } from '../models/key-result-task.interface';
import { environment } from '../../environments/environment';
import { Status } from '../models/Status.enum';
import { Priority } from '../models/Priority.enum';

@Injectable({
  providedIn: 'root'
})
export class KeyResultTaskService {
  private apiUrl = `${environment.apiUrl}/api/keyresulttasks`;

  constructor(private http: HttpClient) {}

  getKeyResultTasks(): Observable<KeyResultTask[]> {
    return this.http.get<KeyResultTask[]>(this.apiUrl).pipe(
      map(tasks => tasks.map(task => this.convertDates(task))),
      catchError(error => {
        console.error('Error fetching key result tasks', error);
        return of([]);
      })
    );
  }

  getKeyResultTasksByKeyResultId(keyResultId: string): Observable<KeyResultTask[]> {
    return this.http.get<KeyResultTask[]>(`${this.apiUrl}/keyresult/${keyResultId}`).pipe(
      map(tasks => tasks.map(task => {
        // Ensure task status is properly set from numeric value
        const convertedTask = this.convertDates(task);
        
        // Check raw status value from the server (3 = Completed)
        // We need to do this because the enum conversion might not always work correctly
        if (task.status === 3) {
          convertedTask.status = Status.Completed;
          convertedTask.progress = 100;
        }
        
        return convertedTask;
      })),
      catchError(error => {
        console.error(`Error fetching tasks for key result ${keyResultId}`, error);
        return of([]);
      })
    );
  }

  getKeyResultTaskById(id: string): Observable<KeyResultTask | undefined> {
    return this.http.get<KeyResultTask>(`${this.apiUrl}/${id}`).pipe(
      map(task => this.convertDates(task)),
      catchError(error => {
        console.error(`Error fetching task ${id}`, error);
        return of(undefined);
      })
    );
  }

  createKeyResultTask(task: Omit<KeyResultTask, 'id'>): Observable<KeyResultTask> {
    if (!task.userId) {
      throw new Error('User ID is required to create a task');
    }

    // Ensure we're working with Date objects
    const startedDate = task.startedDate instanceof Date ? task.startedDate : new Date(task.startedDate);
    const endDate = task.endDate instanceof Date ? task.endDate : new Date(task.endDate);

    const taskToSend = {
      ...task, 
      startedDate: startedDate.toISOString(),
      endDate: endDate.toISOString(),
      status: this.convertStatusToEnum(task.status),
      priority: this.convertPriorityToEnum(task.priority)
    };

    console.log('Sending task to API:', taskToSend); // For debugging

    return this.http.post<KeyResultTask>(this.apiUrl, taskToSend).pipe(
      map(response => this.convertDates(response)),
      catchError(error => {
        console.error('Error creating key result task', error);
        throw error;
      })
    );
  }

  updateKeyResultTask(id: string, updates: Partial<KeyResultTask>): Observable<KeyResultTask | undefined> {
    // Create a copy for API submission
    const updatesToSend: any = { ...updates };
    
    // Keep the original updates object with Date types
    const formattedUpdates: Partial<KeyResultTask> = { ...updates };
    
    if (updates.startedDate) {
      updatesToSend.startedDate = this.formatDateToUTC(updates.startedDate);
    }
    
    if (updates.endDate) {
      updatesToSend.endDate = this.formatDateToUTC(updates.endDate);
    }
    
    if (updates.status !== undefined) {
      formattedUpdates.status = this.convertStatusToEnum(updates.status);
      updatesToSend.status = formattedUpdates.status;
    }
    
    if (updates.priority !== undefined) {
      formattedUpdates.priority = this.convertPriorityToEnum(updates.priority);
      updatesToSend.priority = formattedUpdates.priority;
    }

    // Ensure userId is preserved in updates if it exists
    if (updates.userId) {
      updatesToSend.userId = updates.userId;
    }

    return this.http.put<KeyResultTask>(`${this.apiUrl}/${id}`, updatesToSend).pipe(
      map(response => this.convertDates(response)),
      catchError(error => {
        console.error(`Error updating task ${id}`, error);
    return of(undefined);
      })
    );
  }

  deleteKeyResultTask(id: string): Observable<boolean> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`).pipe(
      map(() => true),
      catchError(error => {
        console.error(`Error deleting task ${id}`, error);
        return of(false);
      })
    );
  }

  toggleKeyResultTaskStatus(
    keyResultTaskId: string,
    keyResultId: string,
    objectiveId: string,
    complete: boolean
  ): Observable<KeyResultTask> {
    const url = `${this.apiUrl}/toggle-status`;
    const body = { keyResultTaskId, keyResultId, objectiveId, complete };
    
    return this.http.patch<KeyResultTask>(url, body).pipe(
      map(task => {
        return this.convertDates(task);
      }),
      catchError(error => {
        // If we get a 200 or 204, treat it as success
        if (error.status === 200 || error.status === 204) {
          // Create a default task response with the toggled status
          const defaultTask = {
            id: keyResultTaskId,
            keyResultId: keyResultId,
            status: complete ? Status.Completed : Status.InProgress,
            progress: complete ? 100 : 0,
            startedDate: new Date(),
            endDate: new Date()
          };
          return of(this.convertDates(defaultTask));
        }
        console.error('Error toggling task status:', error);
        throw error;
      })
    );
  }

  private convertStatusToEnum(statusValue: Status | string | undefined): number {
    if (statusValue === undefined) return Status.NotStarted;
    
    if (typeof statusValue === 'string') {
      switch (statusValue) {
        case 'NotStarted': return Status.NotStarted;
        case 'InProgress': return Status.InProgress;
        case 'Completed': return Status.Completed;
        case 'Overdue': return Status.Overdue;
        default: return Status.NotStarted;
      }
    }
    
    return statusValue;
  }

  private convertPriorityToEnum(priorityValue: Priority | string | undefined): number {
    if (priorityValue === undefined) return Priority.Medium;
    
    if (typeof priorityValue === 'string') {
      switch (priorityValue.toLowerCase()) {
        case 'low': return Priority.Low;
        case 'medium': return Priority.Medium;
        case 'high': return Priority.High;
        default: return Priority.Medium;
      }
    }
    
    return priorityValue;
  }

  private formatDateToUTC(date: Date | string | undefined): string | undefined {
    if (!date) return undefined;
    
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toISOString();
  }

  private convertDates(task: any): KeyResultTask {
    if (!task) {
      console.warn('Received null task in convertDates');
      return {
        id: '',
        keyResultId: '',
        userId: '',
        title: '',
        collaboratorId: '',
        startedDate: new Date(),
        endDate: new Date(),
        progress: 0,
        priority: Priority.Medium,
        status: Status.NotStarted,
        isDeleted: false
      };
    }

    return {
      ...task,
      startedDate: task.startedDate ? new Date(task.startedDate) : new Date(),
      endDate: task.endDate ? new Date(task.endDate) : new Date(),
      createdDate: task.createdDate ? new Date(task.createdDate) : undefined,
      modifiedDate: task.modifiedDate ? new Date(task.modifiedDate) : undefined,
      lastActivityDate: task.lastActivityDate ? new Date(task.lastActivityDate) : undefined
    };
  }
}
