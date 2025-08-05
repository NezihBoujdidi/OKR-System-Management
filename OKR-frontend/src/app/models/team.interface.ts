import { User } from "./user.interface";

export interface Team {
  id: string;
  name: string;
  description?: string;
  organizationId: string;
  teamManagerId: string; // User who manages the team
  collaboratorIds: string[]; // Array of user IDs in the team
  isActive: boolean;
  createdDate: Date;
  modifiedDate?: Date;
}

export interface ExtendedTeam extends Team {
  teamMembers?: User[];
  teamManager?: User;
}

export interface UpdateTeamCommand {
  name: string;
  description: string;
  organizationId: string;
  teamManagerId: string;
}
