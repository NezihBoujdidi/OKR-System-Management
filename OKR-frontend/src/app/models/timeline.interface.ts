import { OKRSession } from "./okr-session.interface";

export interface TimelineSession {
    id: string;
    title: string;
    startDate: string;
    endDate: string;
    color: string | undefined;
    duration?: string;
  } 
  
  export interface TimelineConfig {
    sessions: TimelineSession[];
    currentDate?: string;
    showYearNavigation?: boolean;
    currentYear?: number;
  } 