import { Component, OnInit, OnDestroy } from '@angular/core';
import { NotificationService, Notification } from '../../services/notification.service';
import { MeetingService } from '../../services/meeting.service';
import { Subscription } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-meeting-notification',
  templateUrl: './meeting-notification.component.html',
  styleUrls: ['./meeting-notification.component.scss']
})
export class MeetingNotificationComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  private subscription!: Subscription;
  
  constructor(
    private notificationService: NotificationService,
    private meetingService: MeetingService,
    private router: Router
  ) {}
  
  ngOnInit(): void {
    this.subscription = this.notificationService.getNotifications().subscribe(
      notification => {
        this.notifications.push(notification);
        
        // Auto remove notifications after 10 seconds
        setTimeout(() => {
          this.removeNotification(notification);
        }, 10000);
      }
    );
  }
  
  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
  
  removeNotification(notification: Notification): void {
    const index = this.notifications.indexOf(notification);
    if (index > -1) {
      this.notifications.splice(index, 1);
    }
  }
  
  performAction(notification: Notification): void {
    if (notification.actionUrl) {
      // Check if this is a meeting invitation
      if (notification.actionUrl.includes('/meetings/join/')) {
        const meetingId = notification.actionUrl.split('/').pop();
        // Extract meeting ID and open the meeting room
        if (meetingId) {
          const id = parseInt(meetingId, 10);
          const title = notification.message.split(':')[1]?.trim() || 'Meeting';
          
          // Use the embedded meeting room - set host to false for invitees
          this.router.navigate(['/meetings/room', id], { 
            queryParams: { 
              title: title,
              host: 'false' // Invited users are not hosts by default
            }
          });
          
          this.removeNotification(notification);
        }
      } else {
        // Regular navigation
        this.router.navigateByUrl(notification.actionUrl);
        this.removeNotification(notification);
      }
    }
  }
} 