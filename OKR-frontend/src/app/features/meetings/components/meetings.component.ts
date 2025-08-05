import { Component, OnInit } from '@angular/core';
import { MeetingService } from '../services/meeting.service';
import { AuthStateService } from '../../../services/auth-state.service';
import { Meeting, MeetingUser } from '../models/meeting.interface';
import { NotificationService } from '../services/notification.service';

@Component({
  selector: 'app-meetings',
  templateUrl: './meetings.component.html',
  styleUrls: ['./meetings.component.scss']
})
export class MeetingsComponent implements OnInit {
  meetings: Meeting[] = [];
  users: MeetingUser[] = [];
  isLoading = false;
  currentUser: any;
  
  // Quick meeting properties
  quickMeetingId: number | null = null;
  
  // Meeting form data
  newMeeting = {
    title: '',
    description: '',
    scheduledTime: new Date(),
    invitees: [] as number[]
  };
  
  // UI states
  showCreateModal = false;

  constructor(
    private meetingService: MeetingService,
    private authState: AuthStateService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authState.getCurrentUser();
    this.loadMeetings();
    this.loadOrganizationUsers();
  }
  
  // Start an instant meeting with the whole team
  startQuickMeeting(): void {
    // Generate a unique meeting ID for this quick session
    this.quickMeetingId = Math.floor(Math.random() * 100000);
    
    // Open the meeting room
    this.meetingService.openMeetingRoom(
      this.quickMeetingId, 
      `Quick Team Meeting - ${new Date().toLocaleDateString()}`
    );
  }
  
  // Invite a specific team member to join the current quick meeting
  inviteToQuickMeeting(user: MeetingUser): void {
    // If no quick meeting is active, start one
    if (!this.quickMeetingId) {
      this.quickMeetingId = Math.floor(Math.random() * 100000);
    }
    
    // For demo purposes, show a success notification
    this.notificationService.success(
      `Invitation sent to ${user.name} to join the meeting!`
    );
    
    // In a real implementation with WebSockets, we would send an invite to the user
    // For demo, simulate receiving an invitation after 5 seconds (as if from another user)
    setTimeout(() => {
      this.simulateIncomingInvitation();
    }, 5000);
    
    // Open the meeting for the current user
    this.meetingService.openMeetingRoom(
      this.quickMeetingId, 
      `Meeting with ${user.name} - ${new Date().toLocaleDateString()}`
    );
  }
  
  // Simulate receiving an invitation (for demo purposes)
  simulateIncomingInvitation(): void {
    const hostName = 'Jane Smith';
    const meetingId = this.quickMeetingId || Math.floor(Math.random() * 100000);
    const meetingTitle = 'Discussion about OKRs';
    
    this.notificationService.meetingInvitation(
      hostName,
      meetingId,
      meetingTitle
    );
  }
  
  loadMeetings(): void {
    this.isLoading = true;
    // This would fetch from the API in a real implementation
    setTimeout(() => {
      this.meetings = [
        {
          id: 1,
          title: 'Weekly OKR Review',
          description: 'Review progress on key objectives',
          scheduledTime: new Date(new Date().getTime() + 86400000), // Tomorrow
          organizer: {
            id: 1,
            name: 'John Doe',
            email: 'john@example.com'
          },
          attendees: [
            { id: 2, name: 'Jane Smith', email: 'jane@example.com' },
            { id: 3, name: 'Bob Johnson', email: 'bob@example.com' }
          ],
          status: 'scheduled'
        }
      ];
      this.isLoading = false;
    }, 500);
  }
  
  loadOrganizationUsers(): void {
    // Mock data - would come from API
    this.users = [
      { id: 2, name: 'Jane Smith', email: 'jane@example.com', role: 'TeamManager' },
      { id: 3, name: 'Bob Johnson', email: 'bob@example.com', role: 'Collaborator' },
      { id: 4, name: 'Alice Williams', email: 'alice@example.com', role: 'Collaborator' }
    ];
  }
  
  openCreateMeetingModal(): void {
    this.showCreateModal = true;
    // Reset form
    this.newMeeting = {
      title: '',
      description: '',
      scheduledTime: new Date(),
      invitees: []
    };
  }
  
  closeCreateMeetingModal(): void {
    this.showCreateModal = false;
  }
  
  toggleUserSelection(userId: number): void {
    const index = this.newMeeting.invitees.indexOf(userId);
    if (index > -1) {
      this.newMeeting.invitees.splice(index, 1);
    } else {
      this.newMeeting.invitees.push(userId);
    }
  }
  
  createMeeting(): void {
    if (!this.newMeeting.title || this.newMeeting.invitees.length === 0) {
      // Show validation error
      return;
    }
    
    this.isLoading = true;
    
    // This would be an API call in a real implementation
    setTimeout(() => {
      // Create a mock meeting response
      const createdMeeting: Meeting = {
        id: Math.floor(Math.random() * 1000),
        title: this.newMeeting.title,
        description: this.newMeeting.description,
        scheduledTime: this.newMeeting.scheduledTime,
        organizer: {
          id: this.currentUser?.id || 0,
          name: `${this.currentUser?.firstName || ''} ${this.currentUser?.lastName || ''}`,
          email: this.currentUser?.email || ''
        },
        attendees: this.users.filter(user => 
          this.newMeeting.invitees.includes(user.id)
        ),
        status: 'scheduled'
      };
      
      this.meetings.push(createdMeeting);
      this.isLoading = false;
      this.closeCreateMeetingModal();
      
      // Show success message
    }, 1000);
  }
  
  startMeeting(meetingId: number): void {
    // Use the meeting service to open a Jitsi room
    const meeting = this.meetings.find(m => m.id === meetingId);
    if (meeting) {
      this.meetingService.openMeetingRoom(meetingId, meeting.title);
    }
  }
  
  cancelMeeting(meetingId: number): void {
    if (confirm('Are you sure you want to cancel this meeting?')) {
      this.meetings = this.meetings.filter(m => m.id !== meetingId);
      // Would make API call to cancel on the backend
    }
  }
} 