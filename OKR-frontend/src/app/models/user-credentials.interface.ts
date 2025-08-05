export interface UserCredentials {
    userId: string;
    email: string;
    password: string; // In production, this should be hashed
}