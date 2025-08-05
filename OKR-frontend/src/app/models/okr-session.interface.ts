import { User } from "./user.interface";
import { Status } from "./Status.enum";

export interface OKRSession {
  id: string;
  title: string;
  description: string;
  startedDate: string | Date;
  endDate: string | Date;
  teamIds: string[];
  userId: string;
  userName?: string;
  lastActivityDate?: Date;
  isActive?: boolean;
  approved?: boolean;
  isDeleted?: boolean;
  color?: string;
  status: Status;
  progress?: number;
  organizationId?: string;
  organizationName?: string;
}

export interface CreateOkrCommand {
  title: string;
  description: string;
  startedDate: Date;
  endDate: Date;
  teamIds?: string[];
  userId: string;
  color?: string;
  status: Status;
  priority?: number;
  organizationId?: string;
}


// For full updates
export interface UpdateOkrCommand {
  userId: string;
  title: string;
  description: string;
  startedDate: Date;
  endDate: Date;
  teamIds?: string[];
  color?: string;
  status: Status;
}

export interface TimelineSession {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  color: string;
  duration?: string;
}