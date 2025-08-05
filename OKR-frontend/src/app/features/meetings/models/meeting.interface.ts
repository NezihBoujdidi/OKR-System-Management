export interface MeetingUser {
  id: number;
  name: string;
  email: string;
  profilePictureUrl?: string;
  role?: string;
}

export interface Meeting {
  id: number;
  title: string;
  description: string;
  scheduledTime: Date;
  duration?: number; // in minutes
  organizer: MeetingUser;
  attendees: MeetingUser[];
  status: 'scheduled' | 'in-progress' | 'completed' | 'canceled';
  roomUrl?: string;
}

export interface MeetingCreationDto {
  title: string;
  description: string;
  scheduledTime: Date;
  duration?: number; // in minutes
  attendeeIds: number[];
  organizationId?: string;
} 