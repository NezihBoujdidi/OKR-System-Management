import { JWTPayload } from 'jose';

export interface JwtConfig {
    secret: string;
    accessTokenExpiration: string; // example: '1h'
    refreshTokenExpiration: string; // example: '7d'
}

export interface JwtPayload extends JWTPayload {
    sub: string; // user id
    email: string;
    role: string;
    type: 'access' | 'refresh';
    exp?: number;
    iat?: number;
    aud?: string | string[];
    iss?: string;
    nbf?: number;
    jti?: string;
}