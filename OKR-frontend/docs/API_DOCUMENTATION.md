# API Documentation

## Base URLs
- Production: `https://api.tensai-okr.com/v1`
- Development: `http://localhost:4200/api/v1`
- Staging: `https://staging-api.tensai-okr.com/v1`

## Authentication

### Login
```http
POST /auth/login
Content-Type: application/json

{
  "email": "string",
  "password": "string"
}

Response (200 OK)
{
  "accessToken": "string",
  "refreshToken": "string",
  "user": {
    "id": "string",
    "email": "string",
    "roles": ["string"]
  }
}
```

### Refresh Token
```http
POST /auth/refresh
Authorization: Bearer {refreshToken}

Response (200 OK)
{
  "accessToken": "string",
  "refreshToken": "string"
}
```

## OKR Sessions

### Create Session
```http
POST /okr-sessions
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "string",
  "description": "string",
  "startDate": "date",
  "endDate": "date",
  "teamId": "string"
}

Response (201 Created)
{
  "id": "string",
  "title": "string",
  "description": "string",
  "startDate": "date",
  "endDate": "date",
  "teamId": "string",
  "status": "draft"
}
```

### Get Sessions
```http
GET /okr-sessions
Authorization: Bearer {token}

Query Parameters:
- page (integer)
- limit (integer)
- status (string)
- teamId (string)
- year (integer)

Response (200 OK)
{
  "data": [{
    "id": "string",
    "title": "string",
    "description": "string",
    "startDate": "date",
    "endDate": "date",
    "teamId": "string",
    "status": "string"
  }],
  "pagination": {
    "page": number,
    "limit": number,
    "total": number
  }
}
```

## Teams

### Create Team
```http
POST /teams
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "string",
  "description": "string",
  "organizationId": "string",
  "members": ["userId"]
}

Response (201 Created)
{
  "id": "string",
  "name": "string",
  "description": "string",
  "organizationId": "string",
  "members": [{
    "id": "string",
    "email": "string",
    "role": "string"
  }]
}
```

### Get Team Members
```http
GET /teams/{teamId}/members
Authorization: Bearer {token}

Response (200 OK)
{
  "members": [{
    "id": "string",
    "email": "string",
    "role": "string",
    "joinedAt": "date"
  }]
}
```

## Organizations

### Create Organization
```http
POST /organizations
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "string",
  "industry": "string",
  "size": "string",
  "country": "string"
}

Response (201 Created)
{
  "id": "string",
  "name": "string",
  "industry": "string",
  "size": "string",
  "country": "string",
  "createdAt": "date"
}
```

## Users

### Create User
```http
POST /users
Authorization: Bearer {token}
Content-Type: application/json

{
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "role": "string",
  "organizationId": "string"
}

Response (201 Created)
{
  "id": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "role": "string",
  "organizationId": "string",
  "createdAt": "date"
}
```

## Error Responses

### Common Error Structure
```json
{
  "error": {
    "code": "string",
    "message": "string",
    "details": {}
  }
}
```

### Status Codes
- 400: Bad Request
- 401: Unauthorized
- 403: Forbidden
- 404: Not Found
- 409: Conflict
- 422: Unprocessable Entity
- 500: Internal Server Error

## Rate Limiting

- Rate limit: 100 requests per minute
- Headers:
  - X-RateLimit-Limit
  - X-RateLimit-Remaining
  - X-RateLimit-Reset

## Websocket Events

### Connection
```javascript
socket.connect('wss://api.tensai-okr.com/ws', {
  token: 'Bearer {token}'
});
```

### Event Types
```typescript
interface WebSocketEvents {
  'session.updated': OkrSession;
  'objective.created': Objective;
  'keyResult.updated': KeyResult;
  'team.memberAdded': TeamMember;
  'notification.created': Notification;
}
```