import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SessionInsightsRequest {
  sessionId: string;
  userContext?: any;
}

export interface SessionInsightsResponse {
  insights: string[];
}

@Injectable({ providedIn: 'root' })
export class OkrAiInsightsService {
  private readonly apiUrl = 'http://localhost:5001/api/ai/session-insights';

  constructor(private http: HttpClient) {}

  getSessionInsights(request: SessionInsightsRequest): Observable<SessionInsightsResponse> {
    return this.http.post<SessionInsightsResponse>(this.apiUrl, request);
  }
}
