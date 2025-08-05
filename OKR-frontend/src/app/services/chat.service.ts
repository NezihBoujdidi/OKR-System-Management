import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, map, Observable, of, Subject, tap, throwError } from 'rxjs';
import { ChatMessage, ChatSession, EntityCreationInfo } from '../models/chat.models.interface';
import { v4 as uuidv4 } from 'uuid';
import { HttpClient } from '@angular/common/http';
import { AuthStateService } from './auth-state.service';
 
export interface MessageEvent {
  type: 'user-message-sent' | 'bot-message-received';
  messageId: string;
}

export interface UserContext {
  userId?: string;
  username?: string;
  organizationId?: string;
}
 
@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private sessions: ChatSession[] = [];
  private currentSession = new BehaviorSubject<ChatSession | null>(null);
  private sessionsSubject = new BehaviorSubject<ChatSession[]>([]);
  private messageEvents = new Subject<MessageEvent>();
  private apiUrl = 'http://localhost:5001/api/ai/chat';
  private apiBaseUrl = 'http://localhost:5001/api/ai'; // Base URL for all AI endpoints
 
  constructor(
    private http: HttpClient,
    private authState: AuthStateService
  ) {
    // Load conversation history instead of creating a new session
    this.loadConversationHistory().subscribe();
  }
 
  getCurrentSession(): Observable<ChatSession | null> {
    return this.currentSession.asObservable();
  }
 
  getSessions(): Observable<ChatSession[]> {
    return this.sessionsSubject.asObservable();
  }
 
  getMessageEvents(): Observable<MessageEvent> {
    return this.messageEvents.asObservable();
  }
 
  createSession(): Observable<ChatSession> {
    const newSession: ChatSession = {
      id: uuidv4(),
      title: 'New Chat',
      messages: [],
      timestamp: new Date()
    };
    this.sessions.push(newSession);
    this.sessionsSubject.next(this.sessions);
    this.currentSession.next(newSession);
    return of(newSession);
  }
 
  setCurrentSession(session: ChatSession): void {
    this.currentSession.next(session);
  }
 
  deleteSession(sessionId: string): Observable<void> {
    this.sessions = this.sessions.filter(s => s.id !== sessionId);
    this.sessionsSubject.next(this.sessions);
    if (this.currentSession.value?.id === sessionId) {
      this.currentSession.next(this.sessions[0] || null);
    }
    return of(void 0);
  }
  
  /**
   * Delete a conversation from the server by conversation ID
   * This calls the ChatController's reset endpoint
   */
  deleteConversation(conversationId: string): Observable<void> {
    console.log(`Deleting conversation with ID: ${conversationId} from server`);
    const url = `${this.apiBaseUrl}/reset`;
    
    return this.http.post<any>(url, { conversationId }).pipe(
      tap(response => {
        console.log('Conversation deletion response:', response);
        
        // Also remove from local sessions
        this.sessions = this.sessions.filter(s => s.id !== conversationId);
        this.sessionsSubject.next(this.sessions);
        
        // If deleted the current session, select another one or create new
        if (this.currentSession.value?.id === conversationId) {
          if (this.sessions.length > 0) {
            this.currentSession.next(this.sessions[0]);
          } else {
            this.createSession().subscribe();
          }
        }
      }),
      map(() => void 0),
      catchError(error => {
        console.error('Error deleting conversation:', error);
        return of(void 0);
      })
    );
  }
  
  /**
   * Delete all conversations from the server
   * This calls the ChatController's reset-all endpoint
   */
  deleteAllConversations(): Observable<void> {
    console.log('Deleting all conversations from server');
    const url = `${this.apiBaseUrl}/reset-all`;
    
    return this.http.post<any>(url, {}).pipe(
      tap(response => {
        console.log('All conversations deletion response:', response);
        
        // Clear local sessions
        this.sessions = [];
        this.sessionsSubject.next(this.sessions);
        
        // Create a new session
        this.createSession().subscribe();
      }),
      map(() => void 0),
      catchError(error => {
        console.error('Error deleting all conversations:', error);
        return of(void 0);
      })
    );
  }
 
  sendMessage(content: string): Observable<void> {
    if (!this.currentSession.value) {
      return of(void 0);
    }
 
    const userMessage: ChatMessage = {
      id: uuidv4(),
      content,
      sender: 'user',
      timestamp: new Date(),
      type: 'text'
    };
    
    // Create a new reference to the session to trigger proper updates
    const session = this.currentSession.value;
    // Create a new array of messages to avoid direct mutation
    const updatedMessages = [...session.messages, userMessage];
    
    // Update the session with new messages
    this.currentSession.next({
      ...session,
      messages: updatedMessages
    });
    
    // Update sessions array with a new reference
    const updatedSessions = this.sessions.map((s: ChatSession) => 
      s.id === session.id ? {...s, messages: updatedMessages} : s
    );
    this.sessions = updatedSessions;
    this.sessionsSubject.next(this.sessions);
   
    // Emit user message sent event
    this.messageEvents.next({
      type: 'user-message-sent',
      messageId: userMessage.id
    });
 
    // Get current user details from AuthStateService
    const currentUser = this.authState.getCurrentUser();
    
    // Create user context from authenticated user
    const userContext = {
      userId: currentUser?.id || '',
      userName: currentUser ? `${currentUser.firstName} ${currentUser.lastName}` : '',
      email: currentUser?.email || '',
      organizationId: currentUser?.organizationId || '',
      role: currentUser?.role || '',
      selectedLLMProvider: "azureopenai"
    };
    
    const requestPayload = {
      message: content,
      userContext: userContext,
      conversationId: session.id,
      llmProvider: "azureopenai"
    };

    console.log('[ChatService] sendMessage called with content:', content);
    console.log('[ChatService] Current session ID for request:', session.id);
    console.log('[ChatService] Request payload to be sent:', JSON.stringify(requestPayload, null, 2));
 
    // Send request to backend
    return this.http.post<any>(this.apiUrl, requestPayload).pipe(
      tap(response => {
        console.log('Backend response:', response);
        if (response) {
          const hasTableData = this.containsTableData(response);
          const hasSuccessMessage = this.containsSuccessMessage(response);
          
          // Add debug logs
          console.log('Has success message?', hasSuccessMessage);
          
          // Special check for task creation responses that don't match our regular patterns
          const isTaskCreationResponse = 
            response.response && 
            (response.response.includes("following tasks have been") || 
             response.response.includes("have created the following tasks") ||
             response.response.match(/tasks? (?:has|have) been successfully created/i) ||
             response.response.match(/(\d+) tasks? (?:has|have) been (created|added)/i));
          
          if (hasSuccessMessage || isTaskCreationResponse) {
            const entityInfo = this.extractEntityInfo(response.response);
            console.log('Extracted entity info:', entityInfo);

            // Create the success message
            const botSuccessMessage: ChatMessage = {
              id: uuidv4(),
              content: response.response || 'No response content',
              sender: 'bot',
              timestamp: new Date(),
              type: 'success',
              entityInfo: entityInfo || undefined
            };
            
            // Get the current session and its messages
            const currentSession = this.currentSession.value;
            if (currentSession) {
              // Create a new messages array with the success message
              const updatedMessages = [...currentSession.messages, botSuccessMessage];
              
              // Update the session with the new messages
              this.currentSession.next({
                ...currentSession,
                messages: updatedMessages
              });
              
              // Update the sessions array
              const updatedSessions = this.sessions.map((s: ChatSession) => 
                s.id === currentSession.id ? {...s, messages: updatedMessages} : s
              );
              this.sessions = updatedSessions;
              this.sessionsSubject.next(this.sessions);
            }
            
            // Emit bot message received event
            this.messageEvents.next({
              type: 'bot-message-received',
              messageId: botSuccessMessage.id
            });
            
            // Check for followup question in the response
            // Example: "Would you like me to help you create objectives for this session?"
            const followupPattern = /Would you like (?:me |to |)(?:to |)(?:help you |)(?:add|create|set up|define)(.*?)\?/i;
            const hasFollowupQuestion = followupPattern.test(response.response);
            
            if (hasFollowupQuestion) {
              // Extract the followup message - everything after the success message's main content
              let followupContent = '';
              
              // Find the position after the success confirmation
              const successPatterns = [
                /has been (successfully |)created\./i,
                /has been created successfully\./i,
                /successfully created(.*?)with/i,
                /I've (successfully |)created/i
              ];
              
              for (const pattern of successPatterns) {
                const match = response.response.match(pattern);
                if (match) {
                  const matchEndIndex = match.index + match[0].length;
                  followupContent = response.response.substring(matchEndIndex).trim();
                  break;
                }
              }
              
              // If we couldn't extract the content, use the part with the question
              if (!followupContent) {
                const followupMatch = response.response.match(followupPattern);
                if (followupMatch) {
                  // Get the sentence containing the followup question
                  const sentences = response.response.split(/(?<=\.|\?|\!)\s+/);
                  followupContent = sentences.find((s: string) => followupPattern.test(s)) || '';
                }
              }
              
              // If we still have a followup message, add it as a separate chat message
              if (followupContent) {
                const botFollowupMessage: ChatMessage = {
                  id: uuidv4(),
                  content: followupContent,
                  sender: 'bot',
                  timestamp: new Date(Date.now() + 100), // Slightly later than the success message
                  type: 'text'
                };
                
                setTimeout(() => {
                  const currentSession = this.currentSession.value;
                  if (currentSession) {
                    // Create a new messages array with the followup message
                    const updatedMessages = [...currentSession.messages, botFollowupMessage];
                    
                    // Update the session with the new messages
                    this.currentSession.next({
                      ...currentSession,
                      messages: updatedMessages
                    });
                    
                    // Update the sessions array
                    const updatedSessions = this.sessions.map((s: ChatSession) => 
                      s.id === currentSession.id ? {...s, messages: updatedMessages} : s
                    );
                    this.sessions = updatedSessions;
                    this.sessionsSubject.next(this.sessions);
                    
                    // Emit bot message received event
                    this.messageEvents.next({
                      type: 'bot-message-received',
                      messageId: botFollowupMessage.id
                    });
                  }
                }, 500); // Short delay to make it appear like a follow-up
              }
            }
          } else {
            // Handle normal message or table data as before
          const botMessage: ChatMessage = {
            id: uuidv4(),
            content: response.response || 'No response content',
            sender: 'bot',
            timestamp: new Date(),
            type: hasTableData ? 'table' : 
                  hasSuccessMessage ? 'success' : 'text',
            tableData: hasTableData ? response : undefined,
            entityInfo: hasSuccessMessage ? this.extractEntityInfo(response.response) || undefined : undefined,
            pdfData: response.pdf
          };
          
          // Log the created message
          console.log('Bot message created:', botMessage);
   
            // Get the current session and its messages
            const currentSession = this.currentSession.value;
            if (currentSession) {
              // Create a new messages array with the bot message
              const updatedMessages = [...currentSession.messages, botMessage];
              
              // Update the session with the new messages
              this.currentSession.next({
                ...currentSession,
                messages: updatedMessages
              });
              
              // Update the sessions array
              const updatedSessions = this.sessions.map((s: ChatSession) => 
                s.id === currentSession.id ? {...s, messages: updatedMessages} : s
              );
              this.sessions = updatedSessions;
              this.sessionsSubject.next(this.sessions);
            }
         
          // Emit bot message received event
          this.messageEvents.next({
            type: 'bot-message-received',
            messageId: botMessage.id
          });
          }
        }
      }),
      map(() => void 0),
      catchError(error => {
        console.error('Error in AI response:', error);
        // Still emit bot message received event to hide loading indicator
        this.messageEvents.next({
          type: 'bot-message-received',
          messageId: 'error'
        });
        return of(void 0);
      })
    );
  }
 
  uploadFile(file: File, message?: string): Observable<any> {
    if (!this.currentSession.value) {
      return of(void 0);
    }
    
    // Validate that it's a PDF file
    if (!file.name.toLowerCase().endsWith('.pdf')) {
      console.error('Only PDF files are supported');
      return throwError(() => new Error('Only PDF files are supported'));
    }
 
    const session = this.currentSession.value;
    // Use the message directly if provided, otherwise use a simple generic message
    const messageText = message ? message : `Analyze this file for me`;
    const userMessage: ChatMessage = {
      id: uuidv4(),
      content: messageText,
      sender: 'user',
      timestamp: new Date(),
      type: 'file',
      fileName: file.name
    };
 
    // Create a new array of messages
    const updatedMessages = [...session.messages, userMessage];
    
    // Update the session with the new messages
    this.currentSession.next({
      ...session,
      messages: updatedMessages
    });
    
    // Update sessions array
    const updatedSessions = this.sessions.map((s: ChatSession) => 
      s.id === session.id ? {...s, messages: updatedMessages} : s
    );
    this.sessions = updatedSessions;
    this.sessionsSubject.next(this.sessions);
    
    // Emit message event for file upload
    this.messageEvents.next({
      type: 'user-message-sent',
      messageId: userMessage.id
    });
    
    // Upload the file to the server - include both conversationId and message as query parameters
    const url = `${this.apiBaseUrl}/documents/upload?conversationId=${encodeURIComponent(session.id)}&userId=${this.authState.getCurrentUser()?.id || ''}`;
    const urlWithMessage = message ? 
      `${url}&message=${encodeURIComponent(message)}` : 
      url;
    
    // Create FormData for the file upload - only the file should be in FormData
    const formData = new FormData();
    formData.append('file', file);
    
    return this.http.post<any>(
      urlWithMessage, 
      formData
    ).pipe(
      map(response => {
        console.log('File upload response:', response);
        
        // Use the actual response from the server
        const responseContent = response && response.response 
          ? response.response 
          : `I've received your PDF file "${file.name}". I'll analyze its contents and help you with any questions you have about it.`;
        
        // Add bot message with the actual response from the server
        const botMessage: ChatMessage = {
          id: uuidv4(),
          content: responseContent,
          sender: 'bot',
          timestamp: new Date(),
          type: 'text'
        };
        
        // Get the current session
        const currentSession = this.currentSession.value;
        if (currentSession) {
          // Create a new messages array with the bot message
          const updatedMessages = [...currentSession.messages, botMessage];
          
          // Update the session with the new messages
          this.currentSession.next({
            ...currentSession,
            messages: updatedMessages
          });
          
          // Update the sessions array
          const updatedSessions = this.sessions.map((s: ChatSession) => 
            s.id === currentSession.id ? {...s, messages: updatedMessages} : s
          );
          this.sessions = updatedSessions;
          this.sessionsSubject.next(this.sessions);
        }
        
        // Emit message event for bot response
        this.messageEvents.next({
          type: 'bot-message-received',
          messageId: botMessage.id
        });
        
        // Return the response for potential further processing
        return response;
      }),
      catchError(error => {
        console.error('Error uploading file:', error);
        
        // Add error message from the bot
        const errorMessage: ChatMessage = {
          id: uuidv4(),
          content: `Sorry, I couldn't process your PDF file. Please try uploading it again or use a different file.`,
          sender: 'bot',
          timestamp: new Date(),
          type: 'text'
        };
        
        // Get the current session
        const currentSession = this.currentSession.value;
        if (currentSession) {
          // Create a new messages array with the error message
          const updatedMessages = [...currentSession.messages, errorMessage];
          
          // Update the session with the new messages
          this.currentSession.next({
            ...currentSession,
            messages: updatedMessages
          });
          
          // Update the sessions array
          const updatedSessions = this.sessions.map((s: ChatSession) => 
            s.id === currentSession.id ? {...s, messages: updatedMessages} : s
          );
          this.sessions = updatedSessions;
          this.sessionsSubject.next(this.sessions);
        }
        
        // Emit message event for bot error response
        this.messageEvents.next({
          type: 'bot-message-received',
          messageId: errorMessage.id
        });
        
        // Rethrow the error for the component to handle
        return throwError(() => error);
      })
    );
  }
 
  /* sendAudioMessage(blob: Blob): Observable<void> {
    if (!this.currentSession.value) {
      return of(void 0);
    }
 
    const message: ChatMessage = {
      id: uuidv4(),
      content: 'Audio message sent',
      sender: 'user',
      timestamp: new Date(),
      type: 'audio'
    };
 
    const session = this.currentSession.value;
    session.messages.push(message);
    this.currentSession.next({...session});
   
    return of(void 0);
  } */

  resetSession(): void {
    // Don't reset all sessions, just create a new one
    this.createSession().subscribe();
  }

  // Helper to detect if response contains table data
  private containsTableData(response: any): boolean {
    if (!response) return false;
    
    // Check if response has data in functionResults
    if (response.functionResults) {
      if (response.functionResults.Sessions ||
          response.functionResults.Objectives ||
          response.functionResults.KeyResults ||
          response.functionResults.Tasks ||
          response.functionResults.Teams) {
        return true;
      }
    }
    
    // Check for specific intents that indicate table data
    if (response.intents && Array.isArray(response.intents)) {
      const tableDataIntents = [
        'GetAllOkrSessions', 
        'GetAllObjectives',
        'GetAllKeyResults',
        'GetAllKeyResultTasks',
        'GetTeamsByOrganizationId',
        'AzureOpenAIFunction'  // Add this to handle the new response format
        // Add more intents as needed
      ];
      
      return response.intents.some((intent: string) => 
        tableDataIntents.includes(intent)
      );
    }
    
    return false;
  }

  // Helper to detect if response contains entity creation or update success message
  private containsSuccessMessage(response: any): boolean {
    if (!response || !response.response) return false;
    
    // Check for patterns like "successfully created", "successfully updated", etc.
    const successPatterns = [
      // Creation patterns
      /successfully created .+/i,
      /successfully added .+/i,
      /created the .+ with/i,
      /I've successfully created .+/i,
      /I've created .+/i,
      /The .+ has been created successfully/i,
      /Your .+ has been successfully created/i,
      /The team ['"](.+?)['"] has been created/i,
      
      // Tasks created patterns
      /The following tasks have been successfully created/i,
      /tasks have been successfully created/i,
      /tasks have been created/i,
      /have created the following tasks/i,
      /I've added the following tasks/i,
      /following tasks? (?:has|have) been successfully created/i,
      
      // Team created with objectives pattern
      /The team ['"](.+?)['"] has been created successfully\. Would you like to add members to this team or set up some objectives\?/i,
      
      // Team deleted pattern
      /I've successfully deleted the team ['"](.+?)['"]/i,
      /successfully deleted the team ['"](.+?)['"]/i,
      
      // Team deleted with cleanup pattern
      /I've successfully deleted the team ['"](.+?)['"]\..*All associated resources have been cleaned up/i,
      
      // Update patterns
      /successfully updated .+/i,
      /updated the .+ with/i,
      /I've successfully updated .+/i,
      /I've updated .+/i,
      /The .+ has been updated successfully/i,
      /Your .+ has been successfully updated/i,
      /The team ['"](.+?)['"] has been updated/i,
      
      // Delete patterns
      /successfully deleted .+/i,
      /deleted the .+ with/i,
      /I've successfully deleted .+/i,
      /I've deleted .+/i,
      /The .+ has been deleted successfully/i,
      /Your .+ has been successfully deleted/i,
      /The team ['"](.+?)['"] has been deleted/i
    ];
    
    return successPatterns.some(pattern => 
      pattern.test(response.response)
    );
  }

  // Helper to extract entity information from success message
  private extractEntityInfo(response: string): EntityCreationInfo | null {
    if (!response) return null;
    
    console.log('Extracting entity info from response:', response.substring(0, 100) + (response.length > 100 ? '...' : ''));
    
    // Check if it's an update, delete, or creation message
    const isUpdate = 
      /successfully updated/i.test(response) || 
      /has been updated/i.test(response) || 
      /I've updated/i.test(response) ||
      /information has been updated/i.test(response);
      
    const isDelete = 
      /successfully deleted/i.test(response) || 
      /has been deleted/i.test(response) || 
      /I've deleted/i.test(response) ||
      /was deleted/i.test(response);
    
    const operation = isDelete ? 'delete' : (isUpdate ? 'update' : 'create');
    const actionWord = isDelete ? 'deleted' : (isUpdate ? 'updated' : 'created');
    
    // Enhanced patterns for team creation
    const teamCreationPatterns = [
      // Pattern for team created with "would you like to add members or set up objectives" format
      /The team ['"](.+?)['"] has been created successfully\. Would you like to add members to this team or set up some objectives\?/i,
      // Simple team creation pattern
      /The team ['"](.+?)['"] has been created successfully/i,
      // Another team creation pattern
      /I've created the team ['"](.+?)['"]/i,
      // Another team creation pattern
      /Successfully created the team ['"](.+?)['"]/i,
      // Another team creation pattern
      /Team ['"](.+?)['"] has been successfully created/i
    ];
    
    // Check all team creation patterns
    for (const pattern of teamCreationPatterns) {
      const match = response.match(pattern);
      if (match) {
        const title = match[1];
        
        // Try to extract description if available
        const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                           response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                           response.match(/with description ['"](.+?)['"]/i)?.[1] || '';
        
        return {
          entityType: 'team',
          operation: 'create',
          title,
          description
        };
      }
    }
    
    // Enhanced patterns for team deletion
    const teamDeletionPatterns = [
      // Standard team deletion pattern
      /I've successfully deleted the team ['"](.+?)['"]/i,
      // Alternative team deletion pattern
      /successfully deleted the team ['"](.+?)['"]/i,
      // Another team deletion pattern
      /The team ['"](.+?)['"] has been deleted/i,
      // Another team deletion pattern
      /Team ['"](.+?)['"] was deleted successfully/i
    ];
    
    // Check all team deletion patterns
    for (const pattern of teamDeletionPatterns) {
      const match = response.match(pattern);
      if (match) {
        const title = match[1];
        
        return {
          entityType: 'team',
          operation: 'delete',
          title,
          description: 'All associated resources have been cleaned up.'
        };
      }
    }
    
    // Enhanced patterns for team updates
    const teamUpdatePatterns = [
      // Standard team update pattern
      /The team ['"](.+?)['"] has been updated successfully/i,
      // Alternative team update pattern
      /Team ['"](.+?)['"] has been updated with/i,
      // Another team update pattern
      /I've updated the team ['"](.+?)['"]/i,
      // Another team update pattern
      /Successfully updated team ['"](.+?)['"]/i,
      // Another team update pattern
      /The team ['"](.+?)['"] information has been updated/i
    ];
    
    // Check all team update patterns
    for (const pattern of teamUpdatePatterns) {
      const match = response.match(pattern);
      if (match) {
        const title = match[1];
        
        // Try to extract description if available
        const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                           response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                           response.match(/with description ['"](.+?)['"]/i)?.[1] || '';
        
        return {
          entityType: 'team',
          operation: 'update',
          title,
          description
        };
      }
    }
    
    // Enhanced patterns for OKR sessions
    const sessionPatterns = [
      // Creation patterns
      /created the OKR session ['"](.+?)['"] with/i,
      /successfully created the OKR session ['"](.+?)['"]/i,
      /I've successfully created the OKR session ['"](.+?)['"]/i,
      /I've created the OKR session ['"](.+?)['"]/i,
      /Your OKR session ['"](.+?)['"] has been successfully created/i,
      /The OKR session ['"](.+?)['"] has been created/i,
      
      // Update patterns
      /updated the OKR session ['"](.+?)['"] with/i,
      /successfully updated the OKR session ['"](.+?)['"]/i,
      /I've successfully updated the OKR session ['"](.+?)['"]/i,
      /I've updated the OKR session ['"](.+?)['"]/i,
      /Your OKR session ['"](.+?)['"] has been successfully updated/i,
      /The OKR session ['"](.+?)['"] has been updated/i,
      
      // Delete patterns
      /deleted the OKR session ['"](.+?)['"] with/i,
      /successfully deleted the OKR session ['"](.+?)['"]/i,
      /I've successfully deleted the OKR session ['"](.+?)['"]/i,
      /I've deleted the OKR session ['"](.+?)['"]/i,
      /Your OKR session ['"](.+?)['"] has been successfully deleted/i,
      /The OKR session ['"](.+?)['"] has been deleted/i
    ];
    
    // Check all session patterns
    for (const pattern of sessionPatterns) {
      const match = response.match(pattern);
      if (match) {
        const title = match[1];
        
        // Try to extract description if available
        const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                           response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                           response.match(/with description ['"](.+?)['"]/i)?.[1] || '';
        
        // Try to extract date range using various patterns
        let startDate: string | undefined;
        let endDate: string | undefined;
        
        const dateRange1 = response.match(/from (\d{4}-\d{2}-\d{2}) to (\d{4}-\d{2}-\d{2})/i);
        const dateRange2 = response.match(/starting on (\d{4}-\d{2}-\d{2}) and ending on (\d{4}-\d{2}-\d{2})/i);
        const dateRange3 = response.match(/from (\w+ \d{1,2}, \d{4}) to (\w+ \d{1,2}, \d{4})/i);
        
        if (dateRange1) {
          startDate = dateRange1[1];
          endDate = dateRange1[2];
        } else if (dateRange2) {
          startDate = dateRange2[1];
          endDate = dateRange2[2];
        } else if (dateRange3) {
          // Convert to ISO format if dates are in Month Day, Year format
          try {
            startDate = new Date(dateRange3[1]).toISOString().split('T')[0];
            endDate = new Date(dateRange3[2]).toISOString().split('T')[0];
          } catch (e) {
            // Keep as is if parsing fails
            startDate = dateRange3[1];
            endDate = dateRange3[2];
          }
        }
        
        // Determine operation based on pattern
        const patternOperation = pattern.toString().includes('created') ? 'create' : 
                                (pattern.toString().includes('updated') ? 'update' : 
                                (pattern.toString().includes('deleted') ? 'delete' : operation));
        
        return {
          entityType: 'okr-session',
          operation: patternOperation,
          title,
          description,
          startDate,
          endDate
        };
      }
    }
    
    // Enhanced patterns for objectives
    const objectivePatterns = [
      // Creation patterns
      /created the objective ['"](.+?)['"] with/i,
      /successfully created the objective ['"](.+?)['"]/i,
      /I've successfully created the objective ['"](.+?)['"]/i,
      /I've created the objective ['"](.+?)['"]/i,
      /Your objective ['"](.+?)['"] has been successfully created/i,
      /The objective ['"](.+?)['"] has been created/i,
      
      // Update patterns
      /updated the objective ['"](.+?)['"] with/i,
      /successfully updated the objective ['"](.+?)['"]/i,
      /I've successfully updated the objective ['"](.+?)['"]/i,
      /I've updated the objective ['"](.+?)['"]/i,
      /Your objective ['"](.+?)['"] has been successfully updated/i,
      /The objective ['"](.+?)['"] has been updated/i,
      
      // Delete patterns
      /deleted the objective ['"](.+?)['"] with/i,
      /successfully deleted the objective ['"](.+?)['"]/i,
      /I've successfully deleted the objective ['"](.+?)['"]/i,
      /I've deleted the objective ['"](.+?)['"]/i,
      /Your objective ['"](.+?)['"] has been successfully deleted/i,
      /The objective ['"](.+?)['"] has been deleted/i
    ];
    
    // Check all objective patterns
    for (const pattern of objectivePatterns) {
      const match = response.match(pattern);
      if (match) {
        const title = match[1];
        
        // Try to extract description if available
        const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                           response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                           response.match(/with description ['"](.+?)['"]/i)?.[1] || '';
        
        // Determine operation based on pattern
        const patternOperation = pattern.toString().includes('created') ? 'create' : 
                                (pattern.toString().includes('updated') ? 'update' : 
                                (pattern.toString().includes('deleted') ? 'delete' : operation));
        
        return {
          entityType: 'objective',
          operation: patternOperation,
          title,
          description
        };
      }
    }
    
    // If we couldn't match any specific pattern, use the general patterns
    // Pattern for teams with the description - with the question about adding members
    const fullTeamMatch = response.match(new RegExp(`The team ['"](.+?)['"] has been ${actionWord} with the description: ['"](.+?)['"](\\.|\s*Would you like to add team members now\\?|)`, 'i'));
    
    if (fullTeamMatch) {
      const title = fullTeamMatch[1];
      const description = fullTeamMatch[2];
      
      return {
        entityType: 'team',
        operation,
        title,
        description
      };
    }
    
    // Pattern for teams with the description - without the question
    const simpleTeamMatch = response.match(new RegExp(`The team ['"](.+?)['"] has been ${actionWord} with the description: ['"](.+?)['"]`, 'i'));
    
    if (simpleTeamMatch) {
      const title = simpleTeamMatch[1];
      const description = simpleTeamMatch[2];
      
      return {
        entityType: 'team',
        operation,
        title,
        description
      };
    }
    
    // Special pattern for deleted OKR session
    const deletedSessionMatch = response.match(/I've successfully deleted the OKR session ['"](.+?)['"] \(ID: (.*?)\)/i) ||
                               response.match(/successfully deleted the OKR session ['"](.+?)['"]/i);
    
    if (deletedSessionMatch && isDelete) {
      const title = deletedSessionMatch[1];
      const description = "All associated resources have been cleaned up.";
      
      return {
        entityType: 'okr-session',
        operation: 'delete',
        title,
        description
      };
    }
    
    // Pattern for key results
    const keyResultMatch = response.match(new RegExp(`${actionWord} the key result ['"](.+?)['"] with`, 'i')) ||
                           response.match(new RegExp(`successfully ${actionWord} the key result ['"](.+?)['"]`, 'i')) ||
                           response.match(new RegExp(`I've ${actionWord} the key result ['"](.+?)['"]`, 'i')) ||
                           response.match(new RegExp(`I've successfully ${actionWord} the key result ['"](.+?)['"]`, 'i')) ||
                           response.match(new RegExp(`Your key result ['"](.+?)['"] has been successfully ${actionWord}`, 'i'));
    
    if (keyResultMatch) {
      const title = keyResultMatch[1];
      const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                         response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                         response.match(/with description ['"](.+?)['"]/i)?.[1];
      
      return {
        entityType: 'keyresult',
        operation,
        title,
        description
      };
    }
    
    // Pattern for tasks
    const taskMatch = response.match(new RegExp(`${actionWord} the task ['"](.+?)['"] with`, 'i')) ||
                      response.match(new RegExp(`successfully ${actionWord} the task ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`I've ${actionWord} the task ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`I've successfully ${actionWord} the task ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`Your task ['"](.+?)['"] has been successfully ${actionWord}`, 'i'));
    
    if (taskMatch) {
      const title = taskMatch[1];
      const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                         response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                         response.match(/with description ['"](.+?)['"]/i)?.[1];
      
      // Try to extract due date if available
      const dueDate = response.match(/due on (\d{4}-\d{2}-\d{2})/i)?.[1] ||
                      response.match(/due date: (\d{4}-\d{2}-\d{2})/i)?.[1];
      
      return {
        entityType: 'task',
        operation,
        title,
        description,
        dueDate
      };
    }
    
    // Pattern for teams (general fallback)
    const teamMatch = response.match(new RegExp(`${actionWord} the team ['"](.+?)['"] with`, 'i')) ||
                      response.match(new RegExp(`successfully ${actionWord} the team ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`I've ${actionWord} the team ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`I've successfully ${actionWord} the team ['"](.+?)['"]`, 'i')) ||
                      response.match(new RegExp(`Your team ['"](.+?)['"] has been successfully ${actionWord}`, 'i'));
    
    if (teamMatch) {
      const title = teamMatch[1];
      const description = response.match(/description: ['"](.+?)['"]/i)?.[1] || 
                         response.match(/with the description: ['"](.+?)['"]/i)?.[1] ||
                         response.match(/with description ['"](.+?)['"]/i)?.[1];
      
      return {
        entityType: 'team',
        operation,
        title,
        description
      };
    }
    
    // General success message pattern as a last resort
    if (isUpdate || isDelete || response.includes('successfully created')) {
      // Try to extract entity type and title
      const entityTypePatterns = [
        { pattern: /team ['"](.+?)['"]/i, type: 'team' },
        { pattern: /okr session ['"](.+?)['"]/i, type: 'okr-session' },
        { pattern: /objective ['"](.+?)['"]/i, type: 'objective' },
        { pattern: /key result ['"](.+?)['"]/i, type: 'keyresult' },
        { pattern: /task ['"](.+?)['"]/i, type: 'task' }
      ];
      
      for (const { pattern, type } of entityTypePatterns) {
        const match = response.match(pattern);
        if (match) {
          return {
            entityType: type as any,
            operation,
            title: match[1],
            description: ''
          };
        }
      }
    }
    
    return null;
  }

  /**
   * Loads conversation history from the backend API
   */
  loadConversationHistory(): Observable<void> {
    const userId = this.authState.getCurrentUser()?.id || '';
    console.log('Loading conversation history for user:', userId);
    if (!userId) {
      console.warn('No user ID found, skipping history load');
      this.createSession().subscribe();
      return of(void 0);
    }
    
    console.log(`Fetching conversation history from ${this.apiBaseUrl}/conversations/user/${userId}`);
    
    return this.http.get<any>(`${this.apiBaseUrl}/conversations/user/${userId}`).pipe(
      tap(response => {
        console.log('Loaded conversation history:', response);
        
        if (response && response.conversations && response.conversations.length > 0) {
          console.log(`Found ${response.conversations.length} conversations in history`);
          
          try {
            // Transform backend conversations to frontend session format
            this.sessions = response.conversations.map((conv: any) => {
              console.log(`Transforming conversation: ${conv.id} - ${conv.title}`);
              console.log(`Contains ${conv.messages?.length || 0} messages`);
              return this.transformConversation(conv);
            });
            
            console.log('Transformed sessions:', this.sessions);
            
            // Set the most recent conversation as current
            const mostRecentSession = this.sessions.sort((a, b) => 
              new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
            )[0];
            
            console.log('Set most recent session as current:', mostRecentSession.id);
            console.log('Session contains', mostRecentSession.messages.length, 'messages');
            
            this.sessionsSubject.next(this.sessions);
            this.currentSession.next(mostRecentSession);
          } catch (error) {
            console.error('Error transforming conversations:', error);
            // If transformation fails, create a new session
            this.createSession().subscribe();
          }
        } else {
          console.log('No conversations found in history, creating new session');
          // If no history, create a new session
          this.createSession().subscribe();
        }
      }),
      map(() => void 0),
      catchError(error => {
        console.error('Error loading conversation history:', error);
        // If loading fails, create a new session
        this.createSession().subscribe();
        return of(void 0);
      })
    );
  }

  /**
   * Transforms a backend conversation to frontend ChatSession format
   */
  private transformConversation(conversation: any): ChatSession {
    try {
      console.log(`Processing conversation ${conversation.id} with ${conversation.messages?.length || 0} messages`);
      
      // Filter out system messages which aren't visible in the chat
      const filteredMessages = conversation.messages.filter((msg: any) => 
        msg.role.label !== 'system'
      );
      
      console.log(`Filtered to ${filteredMessages.length} non-system messages`);
      
      // Transform each message
      const transformedMessages = filteredMessages.map((msg: any) => {
        console.log(`Transforming message with role: ${msg.role.label}, functionName: ${msg.functionName || 'none'}`);
        return this.transformMessage(msg);
      });
      
      return {
        id: conversation.id,
        title: conversation.title,
        timestamp: new Date(conversation.timestamp),
        messages: transformedMessages
      };
    } catch (error) {
      console.error('Error in transformConversation:', error);
      // Return a basic session with the information we have
      return {
        id: conversation.id || uuidv4(),
        title: conversation.title || 'Conversation',
        timestamp: new Date(conversation.timestamp || Date.now()),
        messages: []
      };
    }
  }

  /**
   * Transforms a backend message to frontend ChatMessage format
   */
  private transformMessage(message: any): ChatMessage {
    // Add debug logging to understand what's coming from the backend
    console.log('Transforming message from backend:', {
      role: message.role,
      content: message.content?.substring(0, 100) + (message.content?.length > 100 ? '...' : ''),
      functionName: message.functionName,
      functionOutput: message.functionOutput ? typeof message.functionOutput === 'string' ? 'string' : 'object' : null,
      entityType: message.entityType,
      operation: message.operation
    });

    // Determine the appropriate message type and content
    let type: 'text' | 'table' | 'success' = 'text';
    let content = message.content || '';
    let tableData: any = undefined;
    let entityInfo: EntityCreationInfo | undefined = undefined;
    let pdfData: string | undefined = message.pdfData || message.pdf; // Check for existing pdfData or pdf property
    
    // Check if it's a bot message
    if (message.role?.label === 'assistant') {
      try {
        // First priority: Check for explicit success messages in content
        if (content) {
          const extractedInfo = this.extractEntityInfo(content);
          if (extractedInfo) {
            console.log('Found entity info from content:', extractedInfo);
            type = 'success';
            entityInfo = extractedInfo;
          }
        }
        
        // Second priority: Handle success message with entity type and operation from backend
        if (!entityInfo && message.entityType && message.operation) {
          console.log('Processing success message with entityType and operation from backend');
          type = 'success';
          
          // Try to extract entity info from function output if available
          if (message.functionOutput) {
            try {
              // Parse functionOutput if it's a string, otherwise use directly
              const outputData = typeof message.functionOutput === 'string' 
                ? JSON.parse(message.functionOutput) 
                : message.functionOutput;
              
              // Create entity info from backend data
              entityInfo = {
                entityType: this.mapEntityType(message.entityType),
                operation: this.mapOperation(message.operation),
                title: outputData.Name || outputData.name || outputData.Title || outputData.title || '',
                description: outputData.Description || outputData.description || ''
              };
              
              // Add date information if available
              if (outputData.StartDate || outputData.startDate) {
                entityInfo.startDate = outputData.StartDate || outputData.startDate;
              }
              
              if (outputData.EndDate || outputData.endDate) {
                entityInfo.endDate = outputData.EndDate || outputData.endDate;
              }
              
              console.log('Created entity info from backend data:', entityInfo);
            } catch (e) {
              console.error('Error extracting entity info from function output:', e);
            }
          }
        }
        
        // Third priority: Check for table data
        if (!entityInfo && message.functionOutput) {
          console.log('Checking for table data in function output');
          try {
            // Parse functionOutput if it's a string, otherwise use directly
            const outputData = typeof message.functionOutput === 'string' 
              ? JSON.parse(message.functionOutput) 
              : message.functionOutput;
            
            // Check for teams, sessions, objectives, etc.
            if (outputData.Teams || outputData.teams || 
                outputData.Sessions || outputData.sessions ||
                outputData.Objectives || outputData.objectives ||
                outputData.KeyResults || outputData.keyResults ||
                outputData.Tasks || outputData.tasks) {
              type = 'table';
              tableData = outputData;
              console.log('Found table data in function output');
            }
          } catch (e) {
            console.error('Error parsing function output for table data:', e);
          }
        }
        
        // Fourth priority: Special case for team listings and other patterns based on content
        if (!entityInfo && !tableData && content) {
          // Check for team listings
          if ((content.includes("found") || content.includes("Found")) && 
              (content.includes("teams") || content.includes("Teams"))) {
            console.log('Detected team listing message from content');
            type = 'table';
            tableData = {
              response: content,
              intents: ["ListTeams"]
            };
          }
          // Check for other listing patterns
          else {
            const listingPatterns = [
              /found (\d+) teams? (in|matching)/i,
              /found (\d+) objectives/i,
              /found (\d+) key results/i,
              /found (\d+) (okr )?sessions/i
            ];
            
            const isListingResult = listingPatterns.some(pattern => pattern.test(content));
            
            if (isListingResult) {
              console.log('Detected list results in content');
              type = 'table';
              tableData = {
                response: content,
                intents: ["ListData"]
              };
            }
          }
        }
        
        // Log the final decision for debugging
        console.log('Message transformation result:', {
          type: type,
          hasEntityInfo: !!entityInfo,
          hasTableData: !!tableData
        });
        
      } catch (error) {
        console.error('Error transforming message:', error);
        // If there's an error in parsing, default to text message
        type = 'text';
      }
    }
    
    return {
      id: message.timestamp || Date.now().toString(), // Use timestamp as ID or generate one
      content: content,
      sender: message.role?.label === 'assistant' ? 'bot' : 'user',
      timestamp: new Date(message.timestamp || Date.now()),
      type,
      tableData,
      entityInfo,
      pdfData
    };
  }

  /**
   * Maps backend entity type to frontend entity type
   */
  private mapEntityType(entityType: string): 'okr-session' | 'objective' | 'keyresult' | 'task' | 'team' {
    const map: {[key: string]: 'okr-session' | 'objective' | 'keyresult' | 'task' | 'team'} = {
      'Team': 'team',
      'OkrSession': 'okr-session',
      'Objective': 'objective',
      'KeyResult': 'keyresult',
      'KeyResultTask': 'task'
    };
    return map[entityType] || 'team';
  }

  /**
   * Maps backend operation to frontend operation
   */
  private mapOperation(operation: string): 'create' | 'update' | 'delete' {
    const map: {[key: string]: 'create' | 'update' | 'delete'} = {
      'Create': 'create',
      'Update': 'update',
      'Delete': 'delete'
    };
    return map[operation] || 'create';
  }

  /**
   * Determines if a function returns table data
   */
  private isTableDataFunction(functionName: string): boolean {
    if (!functionName) return false;
    
    const tableDataFunctions = [
      'GetAllOkrSessions',
      'GetOkrSessions',
      'GetAllObjectives',  
      'GetObjectives',
      'GetAllKeyResults',
      'GetKeyResults',
      'GetAllKeyResultTasks',
      'GetKeyResultTasks',
      'GetTeamsByOrganizationId',
      'GetTeams'
    ];
    
    // First check exact matches
    if (tableDataFunctions.includes(functionName)) {
      return true;
    }
    
    // Check for partial matches (case insensitive)
    const lowerFunctionName = functionName.toLowerCase();
    return (
      lowerFunctionName.includes('getteams') || 
      lowerFunctionName.includes('getsessions') || 
      lowerFunctionName.includes('getobjectives') || 
      lowerFunctionName.includes('getkeyresults') || 
      lowerFunctionName.includes('gettasks')
    );
  }

  /**
   * Extracts entity information from function output
   */
  private extractEntityInfoFromOutput(entityType: string, outputData: any): EntityCreationInfo {
    console.log('Extracting entity info from output:', { entityType, outputData });
    
    // Map the backend entity type to the frontend entity type
    const entityTypeMap: {[key: string]: 'okr-session' | 'objective' | 'keyresult' | 'task' | 'team'} = {
      'Team': 'team',
      'OkrSession': 'okr-session',
      'Objective': 'objective',
      'KeyResult': 'keyresult',
      'KeyResultTask': 'task'
    };
    
    // Default to 'team' if the entity type isn't recognized
    const mappedEntityType = entityTypeMap[entityType] || 'team';
    
    // Create the entity info object based on entity type
    const entityInfo: EntityCreationInfo = {
      entityType: mappedEntityType,
      operation: 'create', // This will be overridden by the actual operation in transformMessage
      title: outputData.Name || outputData.name || outputData.Title || outputData.title || '',
      description: outputData.Description || outputData.description || ''
    };
    
    // Add date information for entities that have it
    if (outputData.StartDate || outputData.startDate) {
      entityInfo.startDate = outputData.StartDate || outputData.startDate;
    }
    
    if (outputData.EndDate || outputData.endDate) {
      entityInfo.endDate = outputData.EndDate || outputData.endDate;
    }
    
    console.log('Extracted entity info:', entityInfo);
    return entityInfo;
  }
  
  /**
   * Extracts table data from function output
   */
  private extractTableDataFromOutput(functionName: string, outputData: any): any {
    console.log(`Extracting table data from output for function: ${functionName}`, outputData);
    
    // Create an object that mimics the structure expected by DataTableMessageComponent
    const tableData: any = {
      intents: [functionName] // The component checks for intents to determine data type
    };
    
    try {
      // First, check if the data is already in the expected format with direct properties
      if (outputData.Teams || outputData.teams || 
          outputData.Sessions || outputData.sessions ||
          outputData.Objectives || outputData.objectives ||
          outputData.KeyResults || outputData.keyResults ||
          outputData.Tasks || outputData.keyResultTasks) {
        
        // Simply copy the relevant properties
        if (outputData.Teams) tableData.Teams = outputData.Teams;
        if (outputData.teams) tableData.teams = outputData.teams;
        if (outputData.Count) tableData.Count = outputData.Count;
        if (outputData.count) tableData.count = outputData.count;
        
        if (outputData.Sessions) tableData.Sessions = outputData.Sessions;
        if (outputData.sessions) tableData.sessions = outputData.sessions;
        
        if (outputData.Objectives) tableData.Objectives = outputData.Objectives;
        if (outputData.objectives) tableData.objectives = outputData.objectives;
        
        if (outputData.KeyResults) tableData.KeyResults = outputData.KeyResults;
        if (outputData.keyResults) tableData.keyResults = outputData.keyResults;
        
        if (outputData.Tasks) tableData.Tasks = outputData.Tasks;
        if (outputData.keyResultTasks) tableData.keyResultTasks = outputData.keyResultTasks;
        
        // Return early
        console.log('Table data extracted directly from properties:', tableData);
        return tableData;
      }
      
      // If no direct properties found, proceed with function-specific handling
      switch (functionName.toLowerCase()) {
        case 'getteamsbyorganizationid':
        case 'getteams':
          // Handle both capitalized and non-capitalized property names
          tableData.teams = outputData.Teams || outputData.teams || [];
          tableData.Teams = tableData.teams; // Add both formats to be safe
          tableData.teamsCount = outputData.Count || outputData.count || (tableData.teams?.length || 0);
          tableData.count = tableData.teamsCount; // Add both formats to be safe
          break;
          
        case 'getallokrsessions':
        case 'getokrsessions':
          tableData.sessions = outputData.Sessions || outputData.sessions || [];
          tableData.Sessions = tableData.sessions; 
          tableData.sessionsCount = outputData.Count || outputData.count || (tableData.sessions?.length || 0);
          tableData.count = tableData.sessionsCount;
          break;
          
        case 'getallobjectives':
        case 'getobjectives':
          tableData.objectives = outputData.Objectives || outputData.objectives || [];
          tableData.Objectives = tableData.objectives;
          tableData.objectivesCount = outputData.Count || outputData.count || (tableData.objectives?.length || 0);
          tableData.count = tableData.objectivesCount;
          break;
          
        case 'getallkeyresults':
        case 'getkeyresults':
          tableData.keyResults = outputData.KeyResults || outputData.keyResults || [];
          tableData.KeyResults = tableData.keyResults;
          tableData.keyResultsCount = outputData.Count || outputData.count || (tableData.keyResults?.length || 0);
          tableData.count = tableData.keyResultsCount;
          break;
          
        case 'getallkeyresulttasks':
        case 'getkeyresulttasks':
          tableData.keyResultTasks = outputData.Tasks || outputData.keyResultTasks || [];
          tableData.Tasks = tableData.keyResultTasks;
          tableData.keyResultTasksCount = outputData.Count || outputData.count || (tableData.keyResultTasks?.length || 0);
          tableData.count = tableData.keyResultTasksCount;
          break;
          
        default:
          // For unknown functions, include the whole output
          console.log('Unknown function, copying all data:', functionName);
          Object.assign(tableData, outputData);
      }
    } catch (error) {
      console.error('Error extracting table data:', error);
      // In case of error, include original data
      Object.assign(tableData, outputData);
    }
    
    console.log('Extracted table data:', tableData);
    return tableData;
  }
}



