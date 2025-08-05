import { Injectable } from '@angular/core';
import { Subject, Observable } from 'rxjs';

export interface Notification {
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  actionUrl?: string;
  actionText?: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private notificationSubject = new Subject<Notification>();
  
  constructor() {}
  
  // Get notification observable
  getNotifications(): Observable<Notification> {
    return this.notificationSubject.asObservable();
  }
  
  // Show a notification
  showNotification(notification: Notification): void {
    this.notificationSubject.next(notification);
  }
  
  // Helper methods for different notification types
  success(message: string, actionUrl?: string, actionText?: string): void {
    this.showNotification({
      message,
      type: 'success',
      actionUrl,
      actionText
    });
  }
  
  error(message: string): void {
    this.showNotification({
      message,
      type: 'error'
    });
  }
  
  info(message: string, actionUrl?: string, actionText?: string): void {
    this.showNotification({
      message,
      type: 'info',
      actionUrl,
      actionText
    });
  }
  
  // Specialized meeting notification
  meetingInvitation(hostName: string, meetingId: number, meetingTitle: string): void {
    this.showNotification({
      message: `${hostName} has invited you to join a meeting: ${meetingTitle}`,
      type: 'info',
      actionUrl: `/meetings/join/${meetingId}`,
      actionText: 'Join Now'
    });
  }
} 