import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuthStateService } from '../../../services/auth-state.service';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class MeetingService {
  private apiUrl = `${environment.apiUrl}/meetings`;

  constructor(
    private http: HttpClient,
    private authState: AuthStateService,
    private router: Router
  ) { }

  // Generate a meeting room ID
  generateMeetingRoomId(meetingId: number): string {
    // Create a simple, consistent meeting ID without special characters
    // This avoids authentication issues with Jitsi
    return `okrmeeting${meetingId}`;
  }

  // Open a meeting room in the embedded component
  openMeetingRoom(meetingId: number, meetingTitle: string, isHost: boolean = true): void {
    // Navigate to the meeting room route with the meeting ID, title, and host flag
    this.router.navigate(['/meetings/room', meetingId], { 
      queryParams: { 
        title: meetingTitle,
        host: isHost.toString()
      }
    });
  }

  // External options for opening in a new window (fallback)
  openExternalMeetingRoom(meetingId: number, meetingTitle: string): void {
    const roomName = this.generateMeetingRoomId(meetingId);
    const roomUrl = `https://meet.jit.si/${roomName}`;
    
    // Open Jitsi Meet in a new tab
    const newWindow = window.open(roomUrl, '_blank');
    
    // Set window properties and handle errors
    if (newWindow) {
      newWindow.focus();
    } else {
      alert('Please allow popups for this website to join meetings.');
    }
  }

  // Get all meetings for the current user's organization (mock implementation)
  getMeetings(): Observable<any[]> {
    return of([]);
  }

  // Create a new meeting (mock implementation)
  createMeeting(meetingData: any): Observable<any> {
    return of({ id: Math.floor(Math.random() * 1000), ...meetingData });
  }

  // Get organization users for invitation (mock implementation)
  getOrganizationUsers(): Observable<any[]> {
    return of([]);
  }

  // Cancel a meeting (mock implementation)
  cancelMeeting(meetingId: number): Observable<any> {
    return of({ success: true });
  }
} 