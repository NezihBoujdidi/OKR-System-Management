import { Organization } from "./organization.interface";

export interface User {
    id: string;
    firstName: string;
    lastName: string;
    address: string;
    email: string;
    position: string;
    dateOfBirth: string; // ISO format: "YYYY-MM-DD"
    profilePictureUrl: string;
    isNotificationEnabled: boolean;
    isEnabled: boolean;
    gender: Gender;
    organizationId?: string;
    role: RoleType;
    createdDate: string;
    modifiedDate?: string;
    fullName?: string;
}

export enum Gender {
    Male = 1,
    Female = 2
}

export enum RoleType {
    SuperAdmin = 'SuperAdmin',
    OrganizationAdmin = 'OrganizationAdmin',
    TeamManager = 'TeamManager',
    Collaborator = 'Collaborator'
}
  
export interface UserDetails {
    id: string;
    firstName: string;
    lastName: string;
    address: string;
    email: string;
    position: string;
    dateOfBirth: string;
    profilePictureUrl: string;
    isNotificationEnabled: boolean;
    isEnabled: boolean;
    gender: Gender;
    organizationId?: string;
  }

export interface UserDetailsWithRole extends UserDetails {
  role: RoleType;
}

export interface UpdateUserCommand {
  firstName: string;
  lastName: string;
  email: string;
  address: string;
  position: string;
  dateOfBirth: string;  // ISO format (YYYY-MM-DD)
  profilePictureUrl?: string;
  isNotificationEnabled: boolean;
  isEnabled: boolean;
  gender: Gender;
  organizationId?: string;
}