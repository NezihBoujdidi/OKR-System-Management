import { Injectable } from '@angular/core';
import { User } from '../models/user.interface';
import { JwtConfig, JwtPayload } from '../models/jwt.interface';
import { environment } from '../../environments/environment.development';
import * as jose from 'jose';

@Injectable({
  providedIn: 'root'
})
export class JwtService {
  private config: JwtConfig = environment.jwt;
  private encoder = new TextEncoder();

  private async getSecretKey(): Promise<Uint8Array> {
    return this.encoder.encode(this.config.secret);
  }

  async generateAccessToken(user: User): Promise<string> {
    const payload: JwtPayload = {
      sub: user.id,
      email: user.email,
      role: user.role,
      type: 'access'
    };

    const secretKey = await this.getSecretKey();
    return new jose.SignJWT({...payload})
      .setProtectedHeader({ alg: 'HS256' })
      .setIssuedAt()
      .setExpirationTime(this.config.accessTokenExpiration)
      .sign(secretKey);
  }

  async generateRefreshToken(user: User): Promise<string> {
    const payload: JwtPayload = {
      sub: user.id,
      email: user.email,
      role: user.role,
      type: 'refresh'
    };

    const secretKey = await this.getSecretKey();
    return new jose.SignJWT({...payload})
      .setProtectedHeader({ alg: 'HS256' })
      .setIssuedAt()
      .setExpirationTime(this.config.refreshTokenExpiration)
      .sign(secretKey);
  }

  async verifyToken(token: string): Promise<JwtPayload | null> {
    try {
      const secretKey = await this.getSecretKey();
      const { payload } = await jose.jwtVerify(token, secretKey);
      const decodedPayload: JwtPayload = {
        sub: payload.sub as string,
        email: payload['email'] as string,
        role: payload['role'] as string,
        type: payload['type'] as 'access' | 'refresh',
        exp: payload.exp,
        iat: payload.iat,
        iss: payload.iss,
        aud: payload.aud,
        nbf: payload.nbf,
        jti: payload.jti
      };
      return decodedPayload;
    } catch (error) {
      return null;
    }
  }

  async isTokenExpired(token: string): Promise<boolean> {
    const payload = await this.verifyToken(token);
    if (!payload) return true;

    const now = Math.floor(Date.now() / 1000);
    return (payload.exp ?? 0) < now;
  }

  getPayloadFromToken(token: string): JwtPayload | null {
    try {
      const decoded = jose.decodeJwt(token);
      const payload: JwtPayload = {
        sub: decoded.sub as string,
        email: decoded['email'] as string,
        role: decoded['role'] as string,
        type: decoded['type'] as 'access' | 'refresh',
        exp: decoded.exp,
        iat: decoded.iat,
        iss: decoded.iss,
        aud: decoded.aud,
        nbf: decoded.nbf,
        jti: decoded.jti
      };
      return payload;
    } catch (error) {
      return null;
    }
  }
}