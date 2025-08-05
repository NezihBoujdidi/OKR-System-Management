import { RoleType } from './role-type.enum';
import { Gender, UserDetails, UserDetailsWithRole } from './user.interface';



export interface RegisterUserCommand {
  email: string;
  firstName: string;
  lastName: string;
  address: string;
  dateOfBirth: string; // Will be converted to DateTime in backend
  gender: Gender;
  phoneNumber: string;
  position: string;
  password?: string;
  confirmPassword?: string;
  isEnabled: boolean | null;
  roleName: string;
  teamId?: string;
  organizationId?: string;
  supabaseId?: string;
}

export interface LoginUserCommand {
  email: string;
  password: string;
}

export interface ForgotPasswordCommand {
  email: string;
}

export interface ResetPasswordCommand {
  email: string;
  token: string;
  password: string;
  confirmPassword: string;
}

export interface ConfirmEmailCommand {
  userId: string;
  token: string;
}

export interface RefreshTokenCommand {
  refreshToken: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expires: string;  // Already includes expiration
}

export interface AuthResponse extends LoginResponse {
  user: UserDetailsWithRole;
}

export interface GenerateInvitationLinkCommand {
  email: string;
  roleName: string;
  organizationId: string;
  teamId?: string;
}
