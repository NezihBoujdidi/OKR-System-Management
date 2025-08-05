// Collaborator Performance
export interface CollaboratorPerformanceDto {
  collaboratorId: string;
  performance: number;
}

export interface CollaboratorPerformanceRangeDto {
  collaboratorId: string;
  performanceAllTime: number;
  performanceLast30Days: number;
  performanceLast3Months: number;
}

// Employee Growth Stats
export interface YearlyGrowthDto {
  year: number;
  count: number;
}

export interface MonthlyGrowthDto {
  year: number;
  month: number;
  count: number;
}

export interface EmployeeGrowthStatsDto {
  yearly: YearlyGrowthDto[];
  monthly: MonthlyGrowthDto[];
}

// Team Performance
export interface TeamPerformanceBarDto {
  teamId: string;
  teamName: string;
  performanceAllTime: number;
  performanceLast30Days: number;
  performanceLast3Months: number;
}

// Active OKR Sessions
export interface ActiveOKRSessionsDto {
  activeOKRSessionCount: number;
  activeOKRSessionCountLastMonth: number;
}

// Collaborator Task Status Stats
export interface CollaboratorTaskStatusStatsDto {
  collaboratorId: string;
  notStarted: number;
  inProgress: number;
  completed: number;
  overdue: number;
}

// Active Teams Stats
export interface ActiveTeamsDto {
  activeTeamsCount: number;
  activeTeamsCountLastMonth: number;
}

// Manager Session Stats
export interface ManagerSessionStatsDto {
  activeSessions: number;
  delayedSessions: number;
}

export interface CollaboratorMonthlyPerformanceDto {
  year: number;
  month: number;
  performance: number;
}

// Global Organization OKR Stats
export interface GlobalOrgOkrStatsDto {
  organizationCount: number;
  okrSessionCount: number;
}

// User Growth Stats
export interface UserGrowthStatsDto {
  yearly: YearlyGrowthDto[];
  monthly: MonthlyGrowthDto[];
}

// User Roles Count
export interface UserRolesCountDto {
  organizationAdmins: number;
  teamManagers: number;
  collaborators: number;
}

// Paid Organization Count
export interface OrgPaidPlanCountDto {
  paidOrganizations: number;
}

// Task Details for Collaborator Dashboard
export interface KeyResultTaskDto {
  id: string;
  keyResultId: string;
  userId: string;
  title: string;
  description?: string;
  startedDate: string; // Assuming string from backend, will be Date in KeyResultTask
  endDate: string;   // Assuming string from backend, will be Date in KeyResultTask
  createdDate?: string;
  modifiedDate?: string;
  collaboratorId: string;
  progress: number;
  priority: string; // Assuming string representation of Priority enum
  status: string;   // Assuming string representation of Status enum
  isDeleted: boolean;
}

export interface CollaboratorTaskDetailsDto {
  recentCompletedTasks: KeyResultTaskDto[];
  inProgressTasks: KeyResultTaskDto[];
  overdueTasks: KeyResultTaskDto[];
} 