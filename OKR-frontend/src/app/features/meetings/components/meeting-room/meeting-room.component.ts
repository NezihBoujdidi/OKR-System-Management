import { Component, OnInit, AfterViewInit, OnDestroy, ElementRef, ViewChild, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Location } from '@angular/common';
import { AuthStateService } from '../../../../services/auth-state.service';

// Declare global Jitsi variable
declare var JitsiMeetExternalAPI: any;

@Component({
  selector: 'app-meeting-room',
  templateUrl: './meeting-room.component.html',
  styleUrls: ['./meeting-room.component.scss']
})
export class MeetingRoomComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('meetingContainer') meetingContainer!: ElementRef;
  
  @Input() roomName: string = '';
  @Input() meetingTitle: string = '';
  
  isLoading: boolean = true;
  api: any = null;
  currentUser: any;
  isHost: boolean = true; // Default to host/moderator for better experience
  
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private location: Location,
    private authState: AuthStateService
  ) {}
  
  ngOnInit(): void {
    this.currentUser = this.authState.getCurrentUser();
    
    // Get meeting ID from route params if not provided as Input
    this.route.params.subscribe(params => {
      if (params['id'] && !this.roomName) {
        const meetingId = parseInt(params['id'], 10);
        // IMPORTANT: Use a simple fixed name pattern without special characters or timestamps
        // This is crucial for direct access without authentication
        this.roomName = `okrmeeting${meetingId}`;
      }
      
      // Get title from query params
      this.route.queryParams.subscribe(queryParams => {
        if (queryParams['title'] && !this.meetingTitle) {
          this.meetingTitle = queryParams['title'];
        } else if (!this.meetingTitle) {
          this.meetingTitle = 'Team Meeting';
        }
        
        // Check if user is host/moderator
        if (queryParams['host'] !== undefined) {
          this.isHost = queryParams['host'] === 'true';
        }
      });
    });
  }
  
  ngAfterViewInit(): void {
    // Add a safety timeout to hide loading if Jitsi fails to initialize properly
    const loadingTimeout = setTimeout(() => {
      if (this.isLoading) {
        console.log('Loading timeout reached, forcing loading state to false');
        this.isLoading = false;
      }
    }, 15000); // 15 seconds timeout

    // Check for global JitsiMeetExternalAPI
    if (typeof JitsiMeetExternalAPI !== 'undefined') {
      console.log('Jitsi Meet API already loaded, initializing...');
      this.initializeJitsi();
      clearTimeout(loadingTimeout);
    } else {
      console.error('Jitsi Meet API not found');
      this.isLoading = false;
      clearTimeout(loadingTimeout);
      alert('Unable to load the meeting interface. Please try refreshing the page.');
    }
  }
  
  ngOnDestroy(): void {
    // Properly clean up the Jitsi instance
    if (this.api) {
      this.api.dispose();
      this.api = null;
    }
  }
  
  initializeJitsi(): void {
    const domain = 'meet.jit.si';
    
    try {
      // Generate user display name
      const displayName = this.currentUser ? 
        `${this.currentUser.firstName || ''} ${this.currentUser.lastName || ''}`.trim() || 'OKR User' : 
        'OKR User';
      
      // Use just the domain name without any protocol or path
      const options = {
        roomName: this.roomName,
        width: '100%',
        height: '100%',
        parentNode: this.meetingContainer.nativeElement,
        configOverwrite: {
          prejoinPageEnabled: false,
          startWithAudioMuted: false,
          startWithVideoMuted: true,
          defaultLanguage: 'en',
          disableInviteFunctions: false, // Enable invitations to help invite others
          enableInsecureRoomNameWarning: false, // Disable warning about insecure room names
          enableNoisyMicDetection: true,
          enableNoAudioDetection: true,
          // Auto-join options to skip pre-join screen
          enableWelcomePage: false,
          requireDisplayName: false,
          enableClosePage: false,
          hiddenDomain: domain,
          // Force to be moderator
          startAsModerator: this.isHost,
          // Explicitly disable authentication
          AUTHENTICATION_ENABLE: false
        },
        interfaceConfigOverwrite: {
          TOOLBAR_BUTTONS: [
            'microphone', 'camera', 'desktop', 'fullscreen',
            'hangup', 'profile', 'chat', 'recording',
            'raisehand', 'videoquality', 'filmstrip',
            'tileview', 'download', 'invite'
          ],
          SHOW_JITSI_WATERMARK: false,
          SHOW_WATERMARK_FOR_GUESTS: false,
          APP_NAME: 'OKR Assistant',
          NATIVE_APP_NAME: 'OKR Assistant Meetings',
          PROVIDER_NAME: 'OKR Assistant',
          HIDE_INVITE_MORE_HEADER: false, // Show invite header
          DISABLE_JOIN_LEAVE_NOTIFICATIONS: true,
          DISABLE_FOCUS_INDICATOR: true,
          DEFAULT_BACKGROUND: '#3c3c3c',
          DEFAULT_LOCAL_DISPLAY_NAME: 'Me',
          TOOLBAR_ALWAYS_VISIBLE: true,
          SETTINGS_SECTIONS: ['devices', 'language', 'profile'],
          DEFAULT_REMOTE_DISPLAY_NAME: 'Team Member',
          SHOW_CHROME_EXTENSION_BANNER: false,
          // Disable auth elements
          AUTHENTICATION_ENABLE: false,
          TOOLBAR_TIMEOUT: 4000
        },
        userInfo: {
          displayName: displayName,
          email: this.currentUser?.email || '',
          moderator: this.isHost
        }
      };
       
      // Add a safety timeout for joining
      const joinTimeout = setTimeout(() => {
        if (this.isLoading) {
          console.log('Join timeout reached, forcing loading state to false');
          this.isLoading = false;
        }
      }, 10000); // 10 seconds timeout for joining
      
      this.api = new JitsiMeetExternalAPI(domain, options);
      
      // Force the display name to be set immediately
      this.api.executeCommand('displayName', displayName);
      
      // Set the subject (meeting title)
      this.api.executeCommand('subject', this.meetingTitle);
      
      // Additional commands to bypass join screen
      if (this.isHost) {
        this.api.on('participantRoleChanged', (event: any) => {
          if (event.role === 'moderator') {
            // Add additional moderator commands
            console.log('User is now a moderator');
            // Allow others to join automatically
            this.api.executeCommand('toggleLobby', false);
          }
        });
      }
      
      // Force join (important for bypassing auth screen)
      this.api.on('videoConferenceJoined', () => {
        // Clear join timeout
        clearTimeout(joinTimeout);
        
        // Immediately execute commands when joined
        this.api.executeCommand('avatarUrl', 'https://gravatar.com/avatar/default?d=identicon');
        
        // Turn on device access
        this.api.executeCommand('toggleAudio', true);
        
        // Hide loading state when actually joined
        this.isLoading = false;
        console.log('Successfully joined the meeting');
      });
      
      // Add error handler
      this.api.on('videoConferenceNotJoined', (error: any) => {
        console.error('Failed to join conference', error);
        this.isLoading = false;
        clearTimeout(joinTimeout);
      });
      
      // Register event listeners
      this.api.addEventListeners({
        readyToClose: this.handleClose.bind(this),
        videoConferenceLeft: this.handleLeft.bind(this),
        participantJoined: this.handleParticipantJoined.bind(this),
        participantLeft: this.handleParticipantLeft.bind(this),
        // Add error handling
        errorOccurred: (error: any) => {
          console.error('Jitsi error occurred:', error);
          this.isLoading = false;
        }
      });
    } catch (error) {
      console.error('Failed to initialize Jitsi', error);
      this.isLoading = false;
    }
  }
  
  // Event handlers
  handleClose(): void {
    this.leaveMeeting();
  }
  
  handleJoined(event: any): void {
    console.log('Local user joined the meeting', event);
  }
  
  handleLeft(event: any): void {
    console.log('Local user left the meeting', event);
    this.leaveMeeting();
  }
  
  handleParticipantJoined(event: any): void {
    console.log('Remote participant joined', event);
  }
  
  handleParticipantLeft(event: any): void {
    console.log('Remote participant left', event);
  }
  
  leaveMeeting(): void {
    // Navigate back to meetings page
    this.router.navigate(['/meetings']);
  }
} 