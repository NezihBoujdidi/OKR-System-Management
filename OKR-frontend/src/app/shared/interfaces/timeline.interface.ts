export interface TimelineSession {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  color: string;
}

export interface TimelineConfig {
  sessions: TimelineSession[];
  currentDate?: string;
  showYearNavigation?: boolean;
  currentYear?: number;
} 