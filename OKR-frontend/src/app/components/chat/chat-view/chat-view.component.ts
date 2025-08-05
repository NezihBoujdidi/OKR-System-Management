import { Component, EventEmitter, Input, OnInit, Output, OnDestroy } from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { ChatMessage } from '../../../models/chat.models.interface';

@Component({
  selector: 'app-chat-view',
  templateUrl: './chat-view.component.html',
  styleUrls: ['./chat-view.component.scss']
})
export class ChatViewComponent implements OnInit, OnDestroy {
  @Input() messages: ChatMessage[] = [];
  @Input() loading = false;
  @Input() error: string | null = null;
  @Input() isListening: boolean = false;
  @Input() messageContent: string | null = '';
  @Input() isUploading: boolean = false;
  @Input() isTextToSpeechEnabled: boolean = false;
  @Input() isSpeaking: boolean = false;
  @Input() smartReplies: string[] = [];
  @Input() showSmartReplies: boolean = false;
  
  // Typing Animation Properties
  @Input() isTyping: boolean = false;
  @Input() typingMessage: any = null;
  
  @Output() sendMessageEvent = new EventEmitter<string>();
  @Output() resetChatEvent = new EventEmitter<void>();
  @Output() startSpeechEvent = new EventEmitter<void>();
  @Output() fileUploadEvent = new EventEmitter<{file: File, message: string}>();
  @Output() toggleTtsEvent = new EventEmitter<void>();
  @Output() stopSpeakingEvent = new EventEmitter<void>();
  @Output() useSmartReplyEvent = new EventEmitter<string>();
  @Output() hideSmartRepliesEvent = new EventEmitter<void>();
  @Output() skipTypingEvent = new EventEmitter<void>();
  
  messageInput = new FormControl('', Validators.required);
  
  // Properties for file upload
  selectedFile: File | null = null;
  isFileUploading = false;
  private uploadTimeoutId: any = null;

  constructor() { }

  ngOnInit(): void {
  }
  
  ngOnDestroy(): void {
    // Clear any timeouts when component is destroyed
    if (this.uploadTimeoutId) {
      clearTimeout(this.uploadTimeoutId);
    }
  }
  
  // Handle input setters properly
  @Input() set messageContentSetter(value: string) {
    if (value) {
      this.messageInput.setValue(value);
    }
  }
  
  @Input() set isUploadingSetter(value: boolean) {
    this.isFileUploading = value;
    
    // Reset safety timeout if it exists
    if (this.uploadTimeoutId) {
      clearTimeout(this.uploadTimeoutId);
      this.uploadTimeoutId = null;
    }
    
    // If setting to true, create a safety timeout
    if (value === true) {
      this.uploadTimeoutId = setTimeout(() => {
        console.log('ChatViewComponent: Upload timeout triggered - resetting upload state');
        this.isFileUploading = false;
      }, 20000); // 20 seconds max timeout
    }
    
    // When upload is complete, clear the selected file
    if (value === false && this.selectedFile) {
      this.selectedFile = null;
    }
  }
  
  sendMessage(): void {
    // Don't proceed if loading
    if (this.loading) {
      return;
    }

    const message = this.messageInput.value?.trim() || '';
    
    // If we have a file selected, send both the file and message
    if (this.selectedFile) {
      this.isFileUploading = true;
      this.fileUploadEvent.emit({
        file: this.selectedFile,
        message: message
      });
      
      // Reset the message input but keep the file selection until the upload completes
      // The parent component will handle clearing the file selection when upload is done
      this.messageInput.setValue('');
    } 
    // Otherwise just send the message if it's not empty
    else if (message) {
      this.sendMessageEvent.emit(message);
      this.messageInput.setValue('');
    }
  }

  resetChat(): void {
    this.resetChatEvent.emit();
  }
  
  startSpeechToText(): void {
    this.startSpeechEvent.emit();
  }
  
  // Handle file selection
  handleFileSelection(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Validate that it's a PDF file
      if (!file.name.toLowerCase().endsWith('.pdf')) {
        alert('Only PDF files are supported');
        input.value = '';
        return;
      }
      
      // Store the selected file - don't upload yet
      this.selectedFile = file;
      
      // Reset the input so the same file can be selected again if needed
      input.value = '';
    }
  }
  
  // Cancel file selection
  cancelFileSelection(): void {
    this.selectedFile = null;
  }
  
  // Legacy method for backward compatibility
  handleFileUpload(event: Event): void {
    this.handleFileSelection(event);
  }
  
  // Helper to format file size
  getFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
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

  // Test method to request OKR sessions
  testListOkrSessions(): void {
    this.messageInput.setValue('List all OKR sessions');
    this.sendMessage();
  }

  testListKeyResultTasks() {
    this.messageInput.setValue('List all my tasks');
    this.sendMessage();
  }

  testListTeams() {
    this.messageInput.setValue('List all teams in my organization');
    this.sendMessage();
  }

  toggleTextToSpeech(): void {
    this.toggleTtsEvent.emit();
  }

  stopSpeaking(): void {
    this.stopSpeakingEvent.emit();
  }

  useSmartReply(reply: string): void {
    this.useSmartReplyEvent.emit(reply);
  }

  hideSmartReplies(): void {
    this.hideSmartRepliesEvent.emit();
  }

  skipTypingAnimation(): void {
    this.skipTypingEvent.emit();
  }
} 