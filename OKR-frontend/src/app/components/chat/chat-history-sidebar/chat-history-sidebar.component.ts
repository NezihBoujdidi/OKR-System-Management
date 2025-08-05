import { Component, EventEmitter, OnInit, Output, OnDestroy } from '@angular/core';
import { ChatService } from '../../../services/chat.service';
import { ChatSession } from '../../../models/chat.models.interface';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-chat-history-sidebar',
  templateUrl: './chat-history-sidebar.component.html',
  styleUrls: []
})
export class ChatHistorySidebarComponent implements OnInit, OnDestroy {
  sessions: ChatSession[] = [];
  currentSessionId: string | null = null;
  private sessionsSubscription: Subscription | null = null;
  private currentSessionSubscription: Subscription | null = null;

  @Output() sessionSelected = new EventEmitter<ChatSession>();

  constructor(private chatService: ChatService) { }

  ngOnInit(): void {
    // Subscribe to all sessions
    this.sessionsSubscription = this.chatService.getSessions().subscribe(
      (sessions) => {
        this.sessions = sessions;
      }
    );

    // Subscribe to current session to highlight it
    this.currentSessionSubscription = this.chatService.getCurrentSession().subscribe(
      (session) => {
        this.currentSessionId = session?.id || null;
      }
    );
  }

  ngOnDestroy(): void {
    if (this.sessionsSubscription) {
      this.sessionsSubscription.unsubscribe();
    }
    if (this.currentSessionSubscription) {
      this.currentSessionSubscription.unsubscribe();
    }
  }

  // Select a session
  selectSession(session: ChatSession): void {
    this.chatService.setCurrentSession(session);
    this.sessionSelected.emit(session);
  }

  // Create a new session
  createNewSession(): void {
    this.chatService.createSession().subscribe();
  }

  // Delete a session
  deleteSession(event: Event, sessionId: string): void {
    event.stopPropagation(); // Prevent triggering selectSession
    this.chatService.deleteSession(sessionId).subscribe();
  }

  // Get a preview of the first message or a default title
  getSessionTitle(session: ChatSession): string {
    if (session.messages.length > 0) {
      const firstMessage = session.messages[0].content;
      return firstMessage.length > 25 ? firstMessage.substring(0, 25) + '...' : firstMessage;
    }
    return 'New Chat';
  }
}