export interface TableColumn {
  key: string;         // Property name in data object
  header: string;      // Display name
  type: 'text' | 'date' | 'number' | 'boolean' | 'custom'; // Data type for formatting
  sortable?: boolean;  // Whether column can be sorted
  width?: string;      // Column width
  format?: (value: any, row: any) => string; // Custom formatter
}

export interface TableConfig {
  columns: TableColumn[];
  defaultSort?: {column: string, direction: 'asc' | 'desc'};
  searchFields?: string[]; // Which fields to include in search
}

export interface TableData<T> {
  items: T[];
  totalItems: number;
  pageSize?: number;
  currentPage?: number;
}

export interface OkrSession {
  okrSessionId: string;
  title: string;
  description: string;
  startDate: string;
  endDate: string;
  teamManagerId: string;
  teamManagerName: string;
  createdBy: string;
  createdAt: string;
  color: string;
  status: string;
  progress: number;
  promptTemplate: string | null;
}

export interface Objective {
  objectiveId: string;
  title: string; 
  description: string;
  okrSessionId: string;
  okrSessionTitle: string;
  responsibleTeamId: string;
  responsibleTeamName: string;
  userId: string;
  userName: string;
  startedDate: string;
  endDate: string;
  createdDate: string;
  modifiedDate: string | null;
  status: string;
  priority: string;
  progress: number;
  promptTemplate: string | null;
}

export interface KeyResult {
  keyResultId: string;
  title: string;
  description: string;
  startedDate: string;
  endDate: string;
  objectiveId: string;
  objectiveTitle: string;
  userId: string;
  userName: string;
  status: string;
  progress: number;
  promptTemplate: string | null;
}

export interface KeyResultTask {
  keyResultTaskId: string;
  title: string;
  description: string;
  keyResultId: string;
  keyResultTitle: string;
  userId: string;
  userName: string;
  collaboratorId: string;
  collaboratorName: string;
  startedDate: string;
  endDate: string;
  createdDate: string;
  modifiedDate: string | null;
  progress: number;
  priority: string;
  isDeleted: boolean;
  promptTemplate: string | null;
}

export interface Team {
  teamId: string;
  name: string;
  description: string;
  teamManagerId: string | null;
  createdAt: string;
  members: any[];
} 