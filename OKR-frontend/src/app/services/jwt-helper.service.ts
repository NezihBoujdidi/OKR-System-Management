import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

interface JwtClaims {
  sub: string;        // User ID
  email: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': string;  // Update this
  'OrganizationId': string;  // Update to match the exact claim name from token
  jti: string;        // JWT ID
  exp: number;        // Expiration time
  Permission?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class JwtHelperService {
  
  decodeToken(token: string): JwtClaims | null {
    try {
      return jwtDecode<JwtClaims>(token);
    } catch {
      return null;
    }
  }

  getUserIdFromToken(token: string): string | null {
    const claims = this.decodeToken(token);
    return claims?.sub || null;
  }

  getRole(token: string): string | null {
    const claims = this.decodeToken(token);
    return claims?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || null;
  }

  getExpirationTime(token: string): Date | null {
    const claims = this.decodeToken(token);
    return claims?.exp ? new Date(claims.exp * 1000) : null;
  }

  getOrganizationId(token: string): string | null {
    const claims = this.decodeToken(token);
    console.log('Decoded claims:', claims);
    return claims?.OrganizationId || null;
  }

  getEmail(token: string): string | null {
    const claims = this.decodeToken(token);
    return claims?.email || null;
  }
} 