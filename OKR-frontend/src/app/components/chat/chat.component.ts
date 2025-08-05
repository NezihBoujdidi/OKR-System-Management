import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { ChatService, UserContext } from '../../services/chat.service';
import { ChatMessage, ChatSession } from '../../models/chat.models.interface';
import { FormControl, Validators } from '@angular/forms';
import { AuthStateService } from '../../services/auth-state.service';
import { Subscription as RxSubscription } from 'rxjs';
import { v4 as uuidv4 } from 'uuid';
import { SubscriptionService } from '../../services/subscription.service';
import { Subscription, SubscriptionPlan } from '../../models/subscription.interface';
import { RoleType } from '../../models/user.interface';
import { catchError, switchMap, of } from 'rxjs';
import { Router, ActivatedRoute } from '@angular/router';
import { tap, take } from 'rxjs/operators';
// Add interfaces for the new components
interface Agent {
  id: string;
  name: string;
  icon: string;
  description: string;
}

interface SuggestedPrompt {
  title: string;
  icon: string;
  prompt: string;
}

interface Automation {
  id: string;
  name: string;
  description: string;
  icon: string;
  enabled: boolean;
}


// Add these interfaces at the top of the file, after the existing imports
interface SpeechRecognitionErrorEvent {
  error: string;
  message?: string;
}

interface SpeechRecognitionResult {
  transcript: string;
  confidence: number;
}

interface SpeechRecognitionResultList {
  length: number;
  item(index: number): SpeechRecognitionResult[];
  [index: number]: SpeechRecognitionResult[];
}

interface SpeechRecognitionEvent {
  results: SpeechRecognitionResultList;
  resultIndex: number;
}

interface SpeechRecognition {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  onresult: (event: SpeechRecognitionEvent) => void;
  onerror: (event: SpeechRecognitionErrorEvent) => void;
  onend: () => void;
  start: () => void;
  stop: () => void;
}

interface SpeechSynthesisUtterance {
  text: string;
  voice: SpeechSynthesisVoice | null;
  volume: number;
  rate: number;
  pitch: number;
  onend: (() => void) | null;
  onerror: ((event: any) => void) | null;
}

interface SpeechSynthesisVoice {
  voiceURI: string;
  name: string;
  lang: string;
  localService: boolean;
  default: boolean;
}

declare global {
  interface Window {
    SpeechRecognition: new () => SpeechRecognition;
    webkitSpeechRecognition: new () => SpeechRecognition;
  }
}

// Add this type at the top with other interfaces
type ActiveView = 'chat' | 'prompts'; // | 'agents' | 'automations'; // COMMENTED OUT AGENTS AND AUTOMATIONS

// Add interfaces for the new components
/*
interface Agent {
  id: string;
  name: string;
  icon: string;
  description: string;
}
*/

interface SuggestedPrompt {
  title: string;
  icon: string;
  prompt: string;
}

/*
interface Automation {
  id: string;
  name: string;
  description: string;
  icon: string;
  enabled: boolean;
}
*/

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.scss']
})
export class ChatComponent implements OnInit, OnDestroy {
  // Make Math available to template
  Math = Math;
  
  messages: ChatMessage[] = [];
  sessions: ChatSession[] = [];
  currentSession: ChatSession | null = null;
  messageInput: FormControl;
  loading = false;
  error: string | null = null;
  userContext: UserContext = {};
  activeView: ActiveView = 'chat'; // Default view is chat
  
  // These properties support the different views
  // selectedAgentId: string = 'gpt4'; // Default selected agent - COMMENTED OUT
  userInitials: string = 'U'; // Default user initials
  userName: string = 'User'; // Default username

  // Agents list for the AI Agents tab
  /*
  availableAgents: Agent[] = [
    {
      id: 'gpt4',
      name: 'GPT-4',
      icon: 'assets/img/gpt4-icon.png',
      description: 'Advanced AI with superior reasoning and OKR management capabilities'
    },
    {
      id: 'grok',
      name: 'Grok',
      icon: 'assets/img/grok-icon.png',
      description: 'Real-time AI with up-to-date data on goal tracking and performance metrics'
    },
    {
      id: 'claude',
      name: 'Claude',
      icon: 'assets/img/claude-icon.png',
      description: 'Balanced assistant for everyday OKR planning and management'
    }
  ];
  */

  // Suggested prompts for the Prompts tab
  suggestedPrompts: SuggestedPrompt[] = [
    {
      title: 'Create OKR Session',
      icon: 'tasks',
      prompt: 'Create a new OKR session for Q3 2024 with a focus on increasing market share and improving customer satisfaction'
    },
    {
      title: 'Track Progress',
      icon: 'chart-line',
      prompt: 'Help me track progress on my current OKR for improving product development cycle time'
    },
    {
      title: 'OKR Ideas',
      icon: 'lightbulb',
      prompt: 'Suggest some key results for an objective focused on improving team collaboration'
    },
    {
      title: 'Performance Review',
      icon: 'star',
      prompt: 'Help me prepare for my performance review based on my OKR achievements this quarter'
    },
    {
      title: 'OKR Alignment',
      icon: 'link',
      prompt: 'How do I align my team OKRs with the company\'s strategic objectives?'
    },
    {
      title: 'Improve Metrics',
      icon: 'chart-bar',
      prompt: 'Suggest ways to improve our key results metrics for the customer satisfaction objective'
    }
  ];

  // Automations for the Automations tab
  /*
  automations: Automation[] = [
    {
      id: 'progress-tracking',
      name: 'Weekly Progress Updates',
      description: 'Get automatic weekly updates on OKR progress',
      icon: 'chart-line',
      enabled: true
    },
    {
      id: 'meeting-summary',
      name: 'OKR Meeting Summaries',
      description: 'Automatically generate summaries after OKR review meetings',
      icon: 'clipboard-list',
      enabled: false
    },
    {
      id: 'deadline-alerts',
      name: 'Key Result Deadline Alerts',
      description: 'Get notified 3 days before key result deadlines',
      icon: 'bell',
      enabled: true
    },
    {
      id: 'alignment-check',
      name: 'Team Alignment Check',
      description: 'Monthly analysis of team OKR alignment with company goals',
      icon: 'sitemap',
      enabled: false
    }
  ];
  */
  
  private sessionSubscription: RxSubscription | null = null;
  private sessionsSubscription: RxSubscription | null = null;

  // Recording state
  isRecording: boolean = false;
  recordingTime: number = 0;
  recordingTimer: any = null;
  mediaRecorder: MediaRecorder | null = null;
  audioChunks: Blob[] = [];
  recordedAudio: string | null = null;

  private lastSpeechTime: number = 0;
  private silenceTimer: any = null;
  private recognition: SpeechRecognition | null = null;
  isListening: boolean = false;

  // Delete confirmation modals
  showDeleteConversationModal = false;
  showDeleteAllModal = false;
  conversationToDeleteId: string | null = null;
  isDeletingConversation = false; // New loading state for single deletion
  isDeletingAllConversations = false; // New loading state for delete all

  // New property for loading overlay
  showLoadingOverlay = false;

  // New property for selected file
  selectedFile: File | null = null;

  // Add a new property to track file upload state
  isFileUploading = false;

  // Subscription check related properties
  hasValidSubscription: boolean = false;
  isCheckingSubscription: boolean = true;
  userRole: RoleType | null = null;
  // Text-to-speech properties
  isSpeaking: boolean = false;
  isTextToSpeechEnabled: boolean = false;
  selectedVoice: SpeechSynthesisVoice | null = null;
  availableVoices: SpeechSynthesisVoice[] = [];
  speechRate: number = 1.0;
  speechVolume: number = 0.8;
  showVoiceSettings: boolean = false;

  // Smart reply properties
  smartReplies: string[] = [];
  showSmartReplies: boolean = false;

  // Typing Animation Properties
  isTyping: boolean = false;
  typingMessage: string = '';
  private typingTimer?: any;
  private typingSpeed = 50; // 50ms between words for smooth animation
  private previousMessageCount: number = 0;
  private currentTypingMessageId: string | null = null;
  fullResponseText: string = '';

  constructor(
    private chatService: ChatService,
    private authState: AuthStateService,
    private router: Router,
    private subscriptionService: SubscriptionService,
    private route: ActivatedRoute
  ) {
    this.messageInput = new FormControl('', Validators.required);
  }

  ngOnInit(): void {
    this.setupUserContext();
    this.checkSubscription();
    this.subscribeToMessages();
    this.subscribeToSessions();
    
    // Initialize text-to-speech
    this.initializeTextToSpeech();
    
    // Handle URL parameters for conversation routing
    this.route.params.subscribe(params => {
      const conversationId = params['conversationId'];
      if (conversationId) {
        console.log('🔗 URL contains conversation ID:', conversationId);
        // Wait for sessions to load, then try to find and set the conversation
        this.chatService.getSessions().pipe(take(1)).subscribe(sessions => {
          const targetSession = sessions.find(session => session.id === conversationId);
          if (targetSession) {
            console.log('✅ Found conversation in sessions, setting as current');
            this.chatService.setCurrentSession(targetSession);
            this.setActiveView('chat');
          } else {
            console.log('❌ Conversation not found in user sessions, redirecting to chat home');
            // If conversation not found or doesn't belong to user, redirect to chat home
            this.router.navigate(['/chat'], { replaceUrl: true });
          }
        });
      }
    });
  }
  
  ngOnDestroy(): void {
    this.sessionsSubscription?.unsubscribe();
    this.sessionSubscription?.unsubscribe();
    if (this.recordingTimer) {
      clearInterval(this.recordingTimer);
    }
    if (this.silenceTimer) {
      clearTimeout(this.silenceTimer);
    }
    if (this.recognition) {
      this.recognition.stop();
    }
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      this.mediaRecorder.stop();
    }
    this.cleanupRecording();

    // Cleanup typing animation
    this.stopTypingAnimation();
  }

  setupUserContext(): void {
    const currentUser = this.authState.getCurrentUser();
    
    if (currentUser) {
      this.userContext = {
        userId: currentUser.id,
        username: `${currentUser.firstName} ${currentUser.lastName}`,
        organizationId: currentUser.organizationId
      };
      
      // Set user name and initials
      if (currentUser.firstName && currentUser.lastName) {
        this.userName = `${currentUser.firstName} ${currentUser.lastName}`;
        this.userInitials = `${currentUser.firstName.charAt(0)}${currentUser.lastName.charAt(0)}`;
      }
      
      // Store user role for subscription messaging
      this.userRole = currentUser.role;
    }
  }
  
  checkSubscription(): void {
    const currentUser = this.authState.getCurrentUser();
    
    if (!currentUser) {
      this.isCheckingSubscription = false;
      return;
    }
    
    // SuperAdmin always has access
    if (currentUser.role === RoleType.SuperAdmin) {
      this.hasValidSubscription = true;
      this.isCheckingSubscription = false;
      this.subscribeToMessages();
      this.subscribeToSessions();
      return;
    }
    
    // Check subscription for organization
    if (currentUser.organizationId) {
      this.subscriptionService.isOrganizationSubscribed(currentUser.organizationId)
        .pipe(
          catchError(error => {
            console.error('Error checking subscription:', error);
            return of(null);
          })
        )
        .subscribe(subscription => {
          this.isCheckingSubscription = false;
          
          // Check if subscription exists and is not a free plan
          if (subscription && subscription.plan !== SubscriptionPlan.Free && subscription.isActive) {
            this.hasValidSubscription = true;
            this.subscribeToMessages();
            this.subscribeToSessions();
          } else {
            this.hasValidSubscription = false;
          }
        });
    } else {
      this.isCheckingSubscription = false;
      this.hasValidSubscription = false;
    }
  }
  
  subscribeToMessages(): void {
    this.sessionSubscription = this.chatService.getCurrentSession().subscribe(session => {
      // Update current session reference
      this.currentSession = session;
      
      // Update URL if session exists and we're not already on the correct URL
      if (session && this.route.snapshot.params['conversationId'] !== session.id) {
        console.log('🔗 Updating URL for session:', session.id);
        this.router.navigate(['/chat', session.id], { replaceUrl: true });
      } else if (!session && this.route.snapshot.params['conversationId']) {
        // If no session but URL has conversation ID, navigate to base chat
        console.log('🔗 No session, navigating to base chat URL');
        this.router.navigate(['/chat'], { replaceUrl: true });
      }
      
      if (session?.messages) {
        console.log('📨 Session received:', {
          sessionId: session.id,
          messageCount: session.messages.length,
          previousCount: this.previousMessageCount,
          isFirstLoad: this.previousMessageCount === 0
        });

        // If this is the first time loading messages (initial load or session change)
        if (this.previousMessageCount === 0) {
          console.log('🔄 Initial load - setting up without animation');
          this.messages = session.messages;
          this.previousMessageCount = session.messages.length;
          
          // Generate smart replies for the last bot message if exists
          if (session.messages.length > 0) {
            const lastMessage = session.messages[session.messages.length - 1];
            if (lastMessage.sender === 'bot') {
              this.generateSmartReplies(lastMessage.content);
            }
          }
          return;
        }

        // Check if we have new messages
        if (session.messages.length > this.previousMessageCount) {
          console.log(`🆕 Detected ${session.messages.length - this.previousMessageCount} new message(s)`);
          
          // Only care about the most recent message for animation
          const mostRecentMessage = session.messages[session.messages.length - 1];
          
          // If the most recent message is from the bot, show the typing animation
          if (mostRecentMessage.sender === 'bot') {
            console.log('🤖 Starting typing animation for bot message');
            
            // Store the current typing message ID
            this.currentTypingMessageId = mostRecentMessage.id;
            
            // Show all messages EXCEPT the one being animated
            const allPreviousMessages = session.messages.slice(0, -1);
            this.messages = allPreviousMessages;
            
            // Start typing animation for the newest message
            this.startTypingAnimation(mostRecentMessage.content);
            
            // Update the count to include the new message, even though we're not displaying it yet
            this.previousMessageCount = session.messages.length;
          } else {
            // If user message, just show normally without animation
            console.log('👤 User message - updating normally');
            this.messages = session.messages;
            this.previousMessageCount = session.messages.length;
          }
        } else {
          // No new messages or same count - just update normally if not currently typing
          if (!this.isTyping) {
            console.log('🔄 Same message count - updating display');
            this.messages = session.messages;
          }
        }
      }
    });
  }

  subscribeToSessions(): void {
    this.sessionsSubscription = this.chatService.getSessions().subscribe(
      (sessions: ChatSession[]) => {
        this.sessions = sessions;
        console.log(`Loaded ${sessions.length} sessions from history:`);
        if (sessions.length > 0) {
          // Log titles of loaded sessions
          sessions.forEach(session => {
            console.log(`- ${session.title} (${session.id}): ${session.messages.length} messages`);
          });
        }
      }
    );
  }

  sendMessage(message?: string) {
    // Use the provided message or get it from the input
    const msgText = message || this.messageInput.value?.trim();
    
    // Check if there's a selected file from the file input component
    // The file would be passed from the chat-view component via the handleFileUpload method
    const selectedFile = this.selectedFile;
    
    // Don't proceed if there's nothing to send and no loading in progress
    if ((!msgText && !selectedFile) || this.loading) {
      return;
    }
    
    // Set loading state
    this.loading = true;
    
    // If we have a file, upload it first, then send the message if any
    if (selectedFile) {
      console.log('Uploading file first:', selectedFile.name);
      
      this.chatService.uploadFile(selectedFile).subscribe({
        next: () => {
          console.log('File uploaded successfully');
          
          // Clear the file selection after upload
          this.selectedFile = null;
          
          // If there's also a message, send it after file upload completes
          if (msgText) {
            console.log('Now sending message after file upload');
            this.chatService.sendMessage(msgText).subscribe({
              next: () => {
                this.messageInput.reset();
                this.loading = false;
              },
              error: (error) => {
                console.error('Error sending message after file upload:', error);
                this.error = 'File uploaded but failed to send message';
                this.loading = false;
              }
            });
          } else {
            // No message to send, just reset the UI
            this.messageInput.reset();
            this.loading = false;
          }
        },
        error: (error) => {
          console.error('Error uploading file:', error);
          this.error = 'Failed to upload file. Please try again.';
          this.loading = false;
          this.selectedFile = null; // Clear the file selection on error
        }
      });
    } else {
      // No file, just send the message normally
      console.log('Sending message without file');
      this.chatService.sendMessage(msgText).subscribe({
        next: () => {
          this.messageInput.reset();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error sending message:', error);
          this.error = 'Failed to send message';
          this.loading = false;
        }
      });
    }
  }

  resetChat(): void {
    console.log('🔄 Resetting chat');
    
    // Reset message count
    this.previousMessageCount = 0;
    
    // Clear any active typing animation
    if (this.isTyping) {
      this.skipTypingAnimation();
    }
    
    // Just clear current messages and create a new session
    // The sessions list will be updated via subscription
    this.messages = [];
    this.error = null;
    this.chatService.resetSession();
    
    // Navigate to base chat URL for new conversation
    this.router.navigate(['/chat'], { replaceUrl: true });
  }
  
  // Method to switch between different views
  setActiveView(view: ActiveView): void {
    this.activeView = view;
  }
  
  // New methods for the various views
  /*
  selectAgent(agent: Agent): void {
    this.selectedAgentId = agent.id;
    this.setActiveView('chat');
  }
  */
  
  /*
  getSelectedAgentName(): string {
    const agent = this.availableAgents.find(a => a.id === this.selectedAgentId);
    return agent ? agent.name : 'AI Assistant';
  }
  */
  
  /*
  getSelectedAgentIcon(): string {
    const agent = this.availableAgents.find(a => a.id === this.selectedAgentId);
    return agent ? agent.icon : '';
  }
  */
  
  useSuggestedPrompt(prompt: string): void {
    this.messageInput.setValue(prompt);
    this.setActiveView('chat');
  }
  
  /*
  toggleAutomation(automationId: string): void {
    const automation = this.automations.find(a => a.id === automationId);
    if (automation) {
      automation.enabled = !automation.enabled;
    }
  }
  */
  
  // This method would be called when starting a new conversation
  startNewConversation(): void {
    console.log('🆕 Starting new conversation');
    
    // Reset message count for proper detection
    this.previousMessageCount = 0;
    
    // Clear any active typing animation
    if (this.isTyping) {
      this.skipTypingAnimation();
    }
    
    this.resetChat();
    this.setActiveView('chat');
  }
  
  // Handle file upload event from the chat view component
  handleFileUpload(event: {file: File, message: string}): void {
    if (!event || !event.file) return;
    
    console.log('File selected for upload:', event.file.name);
    // If there's a message with the file, upload with message
    if (event.message) {
      this.uploadFile(event.file, event.message);
    } else {
      // Otherwise just upload the file
      this.uploadFile(event.file);
    }
  }
  
  // File upload method
  uploadFile(file: File, message?: string): void {
    if (!file) return;
    
    // Validate that it's a PDF file
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      this.error = 'Only PDF files are supported';
      return;
    }
    
    console.log('Uploading PDF file:', file.name);
    this.loading = true; // Set loading state when upload starts
    this.isFileUploading = true; // Set file upload state
    
    // Safety timeout - ensure upload indicator doesn't get stuck
    const uploadTimeout = setTimeout(() => {
      if (this.isFileUploading) {
        console.log('Upload indicator safety timeout triggered');
        this.isFileUploading = false;
      }
    }, 15000); // 15 seconds timeout
    
    this.chatService.uploadFile(file, message).subscribe({
      next: () => {
        clearTimeout(uploadTimeout);
        this.loading = false; // Remove loading state when upload completes
        this.isFileUploading = false; // Reset file upload state
        // Clear the selected file
        this.selectedFile = null;
      },
      error: (error) => {
        clearTimeout(uploadTimeout);
        console.error('Error uploading file:', error);
        this.error = 'Failed to upload PDF file. Please try again.';
        this.loading = false; // Remove loading state on error
        this.isFileUploading = false; // Reset file upload state on error
      }
    });
  }
  
  // Voice recording methods
  async startVoiceRecording(): Promise<void> {
    if (this.isRecording) return;
    
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.mediaRecorder = new MediaRecorder(stream);
      this.audioChunks = [];
      
      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.audioChunks.push(event.data);
        }
      };
      
      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/wav' });
        this.recordedAudio = URL.createObjectURL(audioBlob);
        
        // Convert to text
        this.convertSpeechToText(audioBlob);
        
        // Stop all tracks
        stream.getTracks().forEach(track => track.stop());
      };
      
      // Start recording
      this.mediaRecorder.start();
      this.isRecording = true;
      this.recordingTime = 0;
      
      // Start timer
      this.recordingTimer = setInterval(() => {
        this.recordingTime++;
      }, 1000);
      
    } catch (err) {
      console.error('Error accessing microphone:', err);
      alert('Unable to access microphone. Please check your permissions.');
      this.isRecording = false;
    }
  }
  
  stopRecording(): void {
    this.cleanupRecording();
  }
  
  cancelRecording(): void {
    this.cleanupRecording();
    this.messageInput.setValue('');
  }
  
  async convertSpeechToText(audioBlob: Blob): Promise<void> {
    try {
      // Create FormData
      const formData = new FormData();
      formData.append('audio', audioBlob, 'recording.wav');
      
      // Here you would typically send the audio to a server endpoint
      // that handles speech-to-text conversion. For now, we'll use
      // the Web Speech API for client-side conversion
      
      const recognition = new (window.SpeechRecognition || window.webkitSpeechRecognition)();
      recognition.lang = 'en-US';
      recognition.continuous = false;
      recognition.interimResults = false;
      
      recognition.onresult = (event) => {
        const transcript = event.results[0][0].transcript;
        if (transcript) {
          this.messageInput.setValue(transcript);
        }
      };
      
      recognition.onerror = (event) => {
        console.error('Speech recognition error:', event.error);
        alert('Error converting speech to text. Please try again.');
      };
      
      // Play the recorded audio while converting
      if (this.recordedAudio) {
        const audio = new Audio(this.recordedAudio);
        audio.onended = () => {
          recognition.stop();
        };
        
        recognition.start();
        audio.play();
      }
      
    } catch (err) {
      console.error('Speech recognition failed:', err);
      alert('Speech-to-text conversion failed. Please try again or type your message.');
    }
  }
  
  playRecording(): void {
    if (!this.recordedAudio) return;
    
    const audio = new Audio(this.recordedAudio);
    audio.play();
  }
  
  formatRecordingTime(): string {
    const minutes = Math.floor(this.recordingTime / 60);
    const seconds = this.recordingTime % 60;
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  }
  
  // Helper for random waveform heights in the template
  getRandomHeight(): number {
    return 20 + Math.random() * 30;
  }

  // Add the sendRecording method
  sendRecording(): void {
    if (!this.recordedAudio) return;
    
    // Create a message for the recorded audio
    const message: ChatMessage = {
      id: uuidv4(),
      content: `Voice message sent`,
      sender: 'user',
      timestamp: new Date(),
      type: 'audio'
    };
    
    // Add message to current session
    if (this.messages) {
      this.messages.push(message);
    }
    
    // Clear the recorded audio
    this.recordedAudio = null;
    
    // Send message to chat service
    this.loading = true;
    this.chatService.sendMessage(message.content).subscribe({
      error: (err) => {
        console.error('Error sending voice message:', err);
        this.error = 'Failed to send voice message';
        this.loading = false;
      }
    });
  }

  // Add this new method
  cleanupRecording(): void {
    if (this.mediaRecorder) {
      try {
        this.mediaRecorder.stop();
      } catch (err) {
        console.error('Error stopping media recorder:', err);
      }
    }
    
    if (this.recordingTimer) {
      clearInterval(this.recordingTimer);
    }
    
    if (this.silenceTimer) {
      clearInterval(this.silenceTimer);
    }
    
    // Clear all recording-related state
    this.isRecording = false;
    this.recordingTime = 0;
    this.recordingTimer = null;
    this.recordedAudio = null;
    this.audioChunks = [];
    this.mediaRecorder = null;
    this.lastSpeechTime = 0;
    
    // Stop any active media streams
    navigator.mediaDevices.getUserMedia({ audio: true })
      .then(stream => {
        stream.getTracks().forEach(track => track.stop());
      })
      .catch(err => console.error('Error accessing media devices:', err));
  }

  private setupSpeechRecognition(): void {
    // First, clean up any existing recognition instance
    if (this.recognition) {
      try {
        this.recognition.stop();
      } catch (e) {
        // Ignore errors if already stopped
      }
      this.recognition = null;
    }

    // Initialize new speech recognition with better browser compatibility checks
    try {
      const SpeechRecognition = (window as any).SpeechRecognition || 
                               (window as any).webkitSpeechRecognition || 
                               (window as any).mozSpeechRecognition || 
                               (window as any).msSpeechRecognition;
                               
      if (!SpeechRecognition) {
        console.error('Speech recognition not supported in this browser');
        alert('Speech recognition is not supported in your browser. Please try using a browser like Chrome or Edge.');
        return;
      }

      this.recognition = new SpeechRecognition();
      if (this.recognition) {
        this.recognition.lang = 'en-US';
        this.recognition.continuous = false;
        this.recognition.interimResults = true;

        this.recognition.onresult = (event: SpeechRecognitionEvent) => {
          const transcript = Array.from(event.results)
            .map(result => result[0].transcript)
            .join('');
          
          console.log('Speech recognition result:', transcript);
          this.messageInput.setValue(transcript);
        };

        this.recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
          console.error('Speech recognition error:', event.error, event.message);
          if (event.error === 'not-allowed') {
            alert('Microphone access was denied. Please enable microphone permissions in your browser settings.');
          } else if (event.error === 'no-speech') {
            console.log('No speech detected');
          } else {
            alert(`Speech recognition error: ${event.error}`);
          }
          this.isListening = false;
        };

        this.recognition.onend = () => {
          console.log('Speech recognition ended');
          this.isListening = false;
        };
      }
    } catch (error) {
      console.error('Error setting up speech recognition:', error);
      alert('There was an error setting up speech recognition. Please try again or use text input instead.');
      this.isListening = false;
    }
  }

  startSpeechToText(): void {
    if (!this.isListening) {
      try {
        // Use getUserMedia to prompt for microphone permissions first
        navigator.mediaDevices.getUserMedia({ audio: true })
          .then(stream => {
            // Stop these tracks immediately - we just wanted the permission
            stream.getTracks().forEach(track => track.stop());
            
            // Now setup and start speech recognition
            this.setupSpeechRecognition();
            
            // Check if recognition was successfully set up
            if (this.recognition) {
              try {
                this.recognition.start();
                this.isListening = true;
                console.log('Speech recognition started successfully');
              } catch (error) {
                console.error('Error starting speech recognition:', error);
                this.isListening = false;
                
                // If we get an error about recognition already started, try stopping and restarting
                if (error instanceof DOMException && error.name === 'InvalidStateError' && this.recognition) {
                  try {
                    this.recognition.stop();
                    setTimeout(() => {
                      if (this.recognition) {
                        this.recognition.start();
                        this.isListening = true;
                      }
                    }, 100);
                  } catch (e) {
                    console.error('Failed to restart recognition:', e);
                  }
                }
              }
            } else {
              console.error('Speech recognition setup failed');
              alert('Could not initialize speech recognition. Please try again or use text input instead.');
            }
          })
          .catch(err => {
            console.error('Error accessing microphone:', err);
            if (err.name === 'NotAllowedError' || err.name === 'PermissionDeniedError') {
              alert('Microphone access was denied. Please enable microphone permissions in your browser settings.');
            } else {
              alert('Error accessing microphone. Please check your device connections and try again.');
            }
            this.isListening = false;
          });
      } catch (error) {
        console.error('Error setting up microphone access:', error);
        alert('There was an error accessing your microphone. Please try again later.');
        this.isListening = false;
      }
    } else {
      // Stop listening if already active
      if (this.recognition) {
        try {
          this.recognition.stop();
        } catch (e) {
          console.error('Error stopping recognition:', e);
        }
        this.isListening = false;
      }
    }
  }

  adjustTextareaHeight(event: any): void {
    const textarea = event.target;
    
    // Reset height to auto to get the correct scrollHeight
    textarea.style.height = 'auto';
    
    // Check if content has multiple lines
    const hasMultipleLines = textarea.value.includes('\n');
    const contentHeight = textarea.scrollHeight;
    
    // Set new height based on content
    if (hasMultipleLines) {
      // For multiline content, allow scrolling up to max height
      const newHeight = Math.max(52, Math.min(contentHeight, 200));
      textarea.style.height = newHeight + 'px';
    } else {
      // For single line, just fit the content
      textarea.style.height = Math.max(52, contentHeight) + 'px';
    }
    
    // If Shift + Enter was pressed
    if (event.shiftKey && event.key === 'Enter') {
      event.preventDefault();
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const value = textarea.value;
      textarea.value = value.substring(0, start) + '\n' + value.substring(end);
      textarea.selectionStart = textarea.selectionEnd = start + 1;
    }
  }

  // Method to set the current session when clicked in the sidebar
  setCurrentSession(session: ChatSession): void {
    console.log('🔄 Switching to session:', session.id);
    
    // Reset message count to trigger proper initial load
    this.previousMessageCount = 0;
    
    // Clear any active typing animation
    if (this.isTyping) {
      this.skipTypingAnimation();
    }
    
    this.chatService.setCurrentSession(session);
    this.setActiveView('chat'); // Switch to chat view when selecting a session
    
    // URL navigation is now handled automatically in the subscription
  }
  
  /**
   * Delete a specific conversation - shows confirmation modal first
   */
  deleteConversation(sessionId: string): void {
    this.conversationToDeleteId = sessionId;
    this.showDeleteConversationModal = true;
  }
  
  /**
   * Confirms deletion of a conversation after modal confirmation
   */
  confirmDeleteConversation(): void {
    if (this.conversationToDeleteId) {
      this.isDeletingConversation = true; // Set loading state
      this.chatService.deleteConversation(this.conversationToDeleteId).subscribe({
        next: () => {
          console.log('Conversation deleted successfully');
          this.isDeletingConversation = false; // Reset loading state
          this.closeDeleteModals();
        },
        error: (error) => {
          console.error('Error deleting conversation:', error);
          this.isDeletingConversation = false; // Reset loading state in case of error
          this.closeDeleteModals();
        }
      });
    }
  }
  
  /**
   * Delete all conversations - shows confirmation modal first
   */
  deleteAllConversations(): void {
    this.showDeleteAllModal = true;
  }
  
  /**
   * Confirms deletion of all conversations after modal confirmation
   */
  confirmDeleteAllConversations(): void {
    this.isDeletingAllConversations = true; // Set loading state
    this.chatService.deleteAllConversations().subscribe({
      next: () => {
        console.log('All conversations deleted successfully');
        this.isDeletingAllConversations = false; // Reset loading state
        this.closeDeleteModals();
      },
      error: (error) => {
        console.error('Error deleting all conversations:', error);
        this.isDeletingAllConversations = false; // Reset loading state in case of error
        this.closeDeleteModals();
      }
    });
  }
  
  /**
   * Closes all delete confirmation modals
   */
  closeDeleteModals(): void {
    this.showDeleteConversationModal = false;
    this.showDeleteAllModal = false;
    this.conversationToDeleteId = null;
    // Reset loading states when closing modals
    this.isDeletingConversation = false;
    this.isDeletingAllConversations = false;
  }
  
  // Navigate back to the main app
  async navigateToHome(): Promise<void> {
    // Show loading overlay
    this.showLoadingOverlay = true;
    
    // Add a small delay for the animation
    await new Promise(resolve => setTimeout(resolve, 800));
    
    this.router.navigate(['/home']);
  }

  navigateToSubscription(): void {
    this.router.navigate(['/subscription/upgrade']);
  }
  private initializeTextToSpeech(): void {
    if ('speechSynthesis' in window) {
      // Load available voices
      this.loadVoices();
      
      // Some browsers need to load voices asynchronously
      if (window.speechSynthesis.onvoiceschanged !== undefined) {
        window.speechSynthesis.onvoiceschanged = () => {
          this.loadVoices();
        };
      }
    } else {
      console.warn('Text-to-speech not supported in this browser');
    }
  }

  private loadVoices(): void {
    this.availableVoices = window.speechSynthesis.getVoices();
    
    // Select default voice (prefer English voices)
    const englishVoices = this.availableVoices.filter(voice => 
      voice.lang.startsWith('en') && voice.localService
    );
    
    if (englishVoices.length > 0) {
      this.selectedVoice = englishVoices[0];
    } else if (this.availableVoices.length > 0) {
      this.selectedVoice = this.availableVoices[0];
    }
  }

  toggleTextToSpeech(): void {
    this.isTextToSpeechEnabled = !this.isTextToSpeechEnabled;
    
    if (!this.isTextToSpeechEnabled && this.isSpeaking) {
      this.stopSpeaking();
    }
  }

  speakText(text: string): void {
    if (!this.isTextToSpeechEnabled || !('speechSynthesis' in window)) {
      return;
    }

    // Stop any current speech
    this.stopSpeaking();

    // Clean text for better speech (remove markdown, special characters)
    const cleanText = this.cleanTextForSpeech(text);
    
    if (cleanText.trim().length === 0) {
      return;
    }

    const utterance = new SpeechSynthesisUtterance(cleanText);
    
    if (this.selectedVoice) {
      utterance.voice = this.selectedVoice;
    }
    
    utterance.volume = this.speechVolume;
    utterance.rate = this.speechRate;
    utterance.pitch = 1.0;

    utterance.onstart = () => {
      this.isSpeaking = true;
    };

    utterance.onend = () => {
      this.isSpeaking = false;
    };

    utterance.onerror = (event) => {
      console.error('Speech synthesis error:', event);
      this.isSpeaking = false;
    };

    window.speechSynthesis.speak(utterance);
  }

  stopSpeaking(): void {
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      this.isSpeaking = false;
    }
  }

  private cleanTextForSpeech(text: string): string {
    return text
      // Remove code blocks
      .replace(/```[\s\S]*?```/g, ' code block ')
      // Remove inline code
      .replace(/`([^`]+)`/g, '$1')
      // Remove markdown links
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
      // Remove markdown bold/italic
      .replace(/\*\*([^*]+)\*\*/g, '$1')
      .replace(/\*([^*]+)\*/g, '$1')
      // Remove excessive whitespace
      .replace(/\s+/g, ' ')
      .trim();
  }

  testVoice(): void {
    const testMessage = "This is a test of the AI assistant voice. How does it sound?";
    this.speakText(testMessage);
  }

  private generateSmartReplies(lastBotMessage: string): void {
    if (!lastBotMessage) return;

    // Generate contextual replies based on the last bot message
    const replies: string[] = [];

    // Check for specific patterns and generate relevant replies
    if (lastBotMessage.toLowerCase().includes('okr') || lastBotMessage.toLowerCase().includes('objective')) {
      replies.push('Tell me more about that');
      replies.push('How do I implement this?');
      replies.push('What are the next steps?');
    }

    if (lastBotMessage.toLowerCase().includes('team') || lastBotMessage.toLowerCase().includes('collaboration')) {
      replies.push('How can I improve team alignment?');
      replies.push('What metrics should we track?');
      replies.push('Can you help me create a team OKR?');
    }

    if (lastBotMessage.toLowerCase().includes('progress') || lastBotMessage.toLowerCase().includes('track')) {
      replies.push('Show me our current progress');
      replies.push('What are the potential blockers?');
      replies.push('How often should we review this?');
    }

    if (lastBotMessage.toLowerCase().includes('key result') || lastBotMessage.toLowerCase().includes('metric')) {
      replies.push('What other metrics could we use?');
      replies.push('How do we measure this effectively?');
      replies.push('Can you suggest realistic targets?');
    }

    // Add general replies if specific ones weren't triggered
    if (replies.length === 0) {
      replies.push('Can you explain this further?');
      replies.push('What would you recommend?');
      replies.push('How does this apply to our situation?');
    }

    // Add common action replies
    replies.push('Create a new OKR session');
    replies.push('Analyze our current OKRs');

    this.smartReplies = replies.slice(0, 4); // Limit to 4 suggestions
    this.showSmartReplies = this.smartReplies.length > 0;
  }

  useSmartReply(reply: string): void {
    this.messageInput.setValue(reply);
    this.hideSmartReplies();
    // Optionally auto-send the reply
    // this.sendMessage(reply);
  }

  hideSmartReplies(): void {
    this.showSmartReplies = false;
  }

  // Typing Animation Methods
  startTypingAnimation(message: string): void {
    console.log('🎬 Starting typing animation for message:', message);
    
    if (this.typingTimer) {
      clearInterval(this.typingTimer);
    }
    
    this.isTyping = true;
    this.typingMessage = '';
    this.fullResponseText = message;
    
    // Split the message into words
    const words = message.split(' ');
    let currentWordIndex = 0;
    let displayedText = '';
    
    console.log('📝 Total words to animate:', words.length);
    console.log('⏱️ Typing speed:', this.typingSpeed + 'ms between words');
    
    this.typingTimer = setInterval(() => {
      if (currentWordIndex < words.length) {
        // Add the next word with a space (except for the first word)
        if (currentWordIndex > 0) {
          displayedText += ' ';
        }
        displayedText += words[currentWordIndex];
        this.typingMessage = displayedText;
        
        console.log(`📤 Word ${currentWordIndex + 1}/${words.length}: "${words[currentWordIndex]}" | Current text: "${displayedText}"`);
        
        currentWordIndex++;
      } else {
        // Animation complete
        console.log('✅ Typing animation complete');
        this.stopTypingAnimation();
      }
    }, this.typingSpeed);
  }

  stopTypingAnimation(): void {
    if (this.typingTimer) {
      clearInterval(this.typingTimer);
      this.typingTimer = undefined;
    }
    
    this.isTyping = false;
    
    // Restore the complete messages array including the typed message
    this.chatService.getCurrentSession().pipe(
      take(1) // Only take one emission to avoid subscription issues
    ).subscribe(session => {
      if (session?.messages) {
        // Now show all messages including the one that was being animated
        this.messages = session.messages;
        console.log('✅ Full message list restored after typing animation');
        
        // Generate smart replies and trigger TTS for the completed message
        if (this.fullResponseText) {
          this.generateSmartReplies(this.fullResponseText);
          if (this.isTextToSpeechEnabled) {
            this.speakText(this.fullResponseText);
          }
        }
      }
    });
    
    // Clear typing state
    this.typingMessage = '';
    this.fullResponseText = '';
    this.currentTypingMessageId = null;
  }

  skipTypingAnimation(): void {
    if (this.typingTimer) {
      clearInterval(this.typingTimer);
      this.typingTimer = undefined;
    }
    
    if (this.isTyping) {
      console.log('⏭️ Skipping typing animation');
      
      // Restore the complete messages array
      this.chatService.getCurrentSession().pipe(
        take(1) // Only take one emission to avoid subscription issues
      ).subscribe(session => {
        if (session?.messages) {
          // Now show all messages including the one that was being animated
          this.messages = session.messages;
          console.log('✅ Full message list restored after skipping animation');
          
          // Generate smart replies and trigger TTS with full text
          if (this.fullResponseText) {
            this.generateSmartReplies(this.fullResponseText);
            if (this.isTextToSpeechEnabled) {
              this.speakText(this.fullResponseText);
            }
          }
        }
      });
    }
    
    // Clear typing state
    this.isTyping = false;
    this.typingMessage = '';
    this.fullResponseText = '';
    this.currentTypingMessageId = null;
  }
}