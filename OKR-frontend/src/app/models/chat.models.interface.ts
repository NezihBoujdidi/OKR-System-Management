export interface ChatMessage {
    id: string;
    content: string;
    sender: 'user' | 'bot';
    timestamp: Date;
    type?: 'text' | 'file' | 'audio' | 'table' | 'success';
    fileName?: string;
    tableData?: any; // Will store the response with table data
    entityInfo?: EntityCreationInfo; // For success messages
    pdfData?: string; // For base64 encoded PDF data
  }
   
  export interface ChatSession {
    id: string;
    title: string;
    messages: ChatMessage[];
    timestamp: Date;
  }

  export interface EntityCreationInfo {
    entityType: 'okr-session' | 'objective' | 'keyresult' | 'task' | 'team';
    operation: 'create' | 'update' | 'delete'; // Add 'delete' as a valid operation
    title: string;
    description?: string;
    startDate?: string;
    endDate?: string;
    [key: string]: any; // Allow for additional properties
  }