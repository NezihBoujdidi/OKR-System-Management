# Login Component Documentation

## Overview

The Login Component provides the authentication entry point for users of the NXM Tensai OKR Management System. It implements a secure and user-friendly login process that authenticates users and directs them to appropriate sections of the application based on their role.

## Component Structure

### Location
- **Module**: `LoginModule` 
- **Component**: `LoginComponent`
- **Path**: `src/app/features/login/components/login/login.component.ts`

### Dependencies

- **Core Services**:
  - `AuthService` - Handles user authentication and token management
  - `AuthStateService` - Maintains the authentication state of the user
  - `Router` - For navigation after successful login

- **Form Handling**:
  - `ReactiveFormsModule` - Provides reactive form capabilities
  - `FormBuilder` - Used to create the login form

## Features

### 1. Authentication Process

The component handles the core login functionality:
- Collects user credentials (email and password)
- Validates form input
- Submits credentials to the authentication service
- Processes the authentication response
- Redirects users based on their role

### 2. Role-Based Navigation

After successful authentication, users are redirected to different routes based on their role:
- SuperAdmin → `/home`
- OrganizationAdmin → `/home/organizationAdmin`
- TeamManager → `/home/teamManager`
- Collaborator → `/home/collaborator`

### 3. Form Validation

Implements validation for login credentials:
- Required field validation
- Email format validation
- Custom error messages

### 4. Security Features

- Password visibility toggle
- Secure credential handling
<!-- - "Remember Me" functionality -->
- Session management

### 5. User Experience

- Clear error messaging
- Loading state management during authentication
- Responsive design for various device sizes
- Visual feedback during form submission

## Form Structure

The login form consists of the following fields:

### User Credentials
- Email Address (required, email format)
- Password (required)
<!-- - Remember Me (optional checkbox) -->

## Usage

### Routing

The component is accessible via the following routes:
- `/login` - Main login page
- The component is part of a feature module with additional routes for password recovery:
  - `/login/forgot-password`
  - `/login/reset-password`

### Authentication Flow

1. **Form Initialization**:
   - Initialize login form with email, password, and remember me fields
   - Set up validators

2. **Form Validation**:
   - Client-side validation for all fields
   - Display appropriate error messages

3. **Authentication Process**:
   - Submit credentials to AuthService
   - Process authentication response
   - Redirect based on user role
   - Display error message on authentication failure

## Error Handling

- Form validation errors
- Authentication failure handling
- Network error handling
- Clear error messaging

## State Management

Key component state variables:

- `loginForm`: FormGroup containing form controls for email, password, and remember me
- `errorMessage`: Stores validation or API errors
- `isLoading`: Tracks submission state for button disabling and spinner display
- `showPassword`: Controls password visibility toggle

## Custom Methods

| Method | Parameters | Return | Description |
|--------|------------|--------|-------------|
| `onSubmit()` | None | void | Handles form submission and authentication process |
| `togglePasswordVisibility()` | None | void | Toggles password field visibility between text and password type |

## Component Layout

The component features a two-column layout:

### Left Column
- Logo and heading section
- Login form with:
  - Email input field
  - Password input field with visibility toggle
  - Remember Me checkbox
  - Forgot password link
  - Error message display
  - Login button with loading indicator
  - Link to signup page

### Right Column
- Platform features highlights:
  - Track Your Goals
  - Team Collaboration
  - Real-time Analytics
- Each feature includes an icon, heading, and description

## Example Usage

```typescript
// In a parent component or routing module
this.router.navigate(['/login']);

// With query parameters
this.router.navigate(['/login'], { 
  queryParams: { redirectUrl: '/dashboard' } 
});
```

## Best Practices & Notes

1. **Security Considerations**:
   - Credentials are never stored in local storage (except when "Remember Me" is checked)
   - Failed login attempts could be tracked to prevent brute force attacks
   - HTTPS should be enforced for all authentication requests

2. **Performance Optimization**:
   - The component implements a loading state to provide visual feedback during authentication
   - Error messages are displayed inline to prevent page reloads
   - Form controls are validated in real-time for immediate user feedback

3. **Accessibility Features**:
   - Form fields have associated labels
   - Error messages are properly associated with input fields
   - Visual indicators are accompanied by text for screen readers
   - Focus states are clearly visible
   - Loading states are communicated appropriately

4. **Integration with Other Components**:
   - The login component connects with ForgotPasswordComponent and ResetPasswordComponent
   - It provides navigation to the SignupComponent
   - After successful authentication, it interacts with the AuthStateService to determine routing
