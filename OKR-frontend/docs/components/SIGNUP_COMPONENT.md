# Signup Component Documentation

## Overview

The Signup Component is a comprehensive registration form that allows new users to create accounts within the NXM Tensai OKR Management System. It supports two distinct registration flows:

1. **Standard Registration** - New users can register with their personal details and create a new organization
2. **Invitation-based Registration** - Users can register via an invitation link with a pre-defined role in an existing organization

## Component Structure

### Location
- **Module**: `SignupModule` 
- **Component**: `SignupComponent`
- **Path**: `src/app/features/signup/components/signup.component.ts`

### Dependencies

- **Core Services**:
  - `AuthService` - Handles user registration and authentication
  - `OrganizationService` - Manages organization creation and updates
  - `UserService` - Manages user profile updates
  - `Router` & `ActivatedRoute` - For navigation and route parameter handling

- **Form Handling**:
  - `ReactiveFormsModule` - Provides reactive form capabilities
  - `FormBuilder` - Used to create and manage complex forms

## Features

### 1. Dynamic Form Management

The component dynamically adjusts its form fields based on whether the user is registering via an invitation or as a new organization admin:

- When registered via invitation (`hasInviteKey = true`):
  - Only personal details are required
  - Organization ID details are pre-populated from the invitation
  - The user's role is determined by the invitation

- When registering as a new organization (`hasInviteKey = false`):
  - Both personal and organization details are required
  - User is automatically assigned the Organization Admin role
  - The account is initially disabled pending admin approval

### 2. Form Validation

Comprehensive form validation includes:

- Required field validation
- Email format validation
- Password complexity requirements (uppercase, lowercase, numbers, special characters)
- Password matching validation
- Phone number format validation
- Terms and conditions agreement

### 3. Security Features

- Password visibility toggle
- Secure password handling
- Invitation token validation
- CSRF protection

### 4. User Experience

- Clear error messages for each field
- Loading state management during submission
- Success message and redirection after successful registration
- Responsive design for various device sizes
- Elegant visual styling with animation effects

## Form Structure

The form consists of two main sections:

### Personal Information

- First Name & Last Name
- Email Address
- Physical Address
- Date of Birth
- Gender
- Phone Number
- Position/Role
- Password & Password Confirmation

### Organization Information (only for non-invitation signups)

- Organization Name
- Organization Description
- Country
- Industry
- Organization Size
- Organization Contact Email
- Organization Phone Number

## Usage

### Routing

The component is accessible via the following routes:

- `/signup` - Standard registration flow
- `/signup?token=<invitationToken>` - Invitation-based registration

### Registration Flow

1. **Form Initialization**:
   - Check for invitation token
   - Initialize appropriate form fields based on registration type

2. **Form Validation**:
   - Client-side validation for all fields
   - Custom validators for password complexity and matching

3. **Submission Process**:
   - Standard Registration:
     1. Register user (disabled by default)
     2. Create organization (inactive by default)
     3. Update user with organization ID
     4. Display success message
   
   - Invitation Registration:
     1. Validate invitation token
     2. Verify email matches invitation
     3. Register user with predefined role and organization
     4. Redirect to login page

## Error Handling

- Form validation errors are displayed inline below each field
- API errors are captured and displayed as notifications
- Network errors are handled gracefully

## State Management

Key component state variables:

- `signupForm`: FormGroup containing all form controls
- `errorMessage`: Stores validation or API errors
- `isLoading`: Tracks submission state for button disabling
- `hasInviteKey`: Determines which form fields to display
- `registrationSuccess`: Controls success message display

## Custom Methods

| Method | Parameters | Return | Description |
|--------|------------|--------|-------------|
| `onSubmit()` | None | void | Handles form submission logic |
| `isFieldInvalid(fieldName)` | string | boolean | Checks if a form field is invalid and touched |
| `getErrorMessage(fieldName)` | string | string | Returns appropriate error message for a field |
| `togglePasswordVisibility(field)` | 'password' \| 'confirmPassword' | void | Toggles password field visibility |
| `navigateToLogin()` | None | void | Navigates user to login page |

## Example Usage

```typescript
// In a parent component or routing module
this.router.navigate(['/signup']);

// With invitation token
this.router.navigate(['/signup'], { queryParams: { token: 'invitation-token-here' } });
```

## Best Practices & Notes

1. **Security Considerations**:
   - Never store passwords in local storage
   - Always validate invitation tokens server-side
   - Implement rate limiting for registration attempts

2. **Maintenance**:
   - When adding new form fields, update both the component HTML and TS files
   - Remember to update validation logic for any new fields
   - Consider the mobile experience when adding form elements

3. **Performance**:
   - The component uses OnPush change detection for better performance
   - Form controls are only validated when touched to improve user experience

4. **Accessibility**:
   - All form fields have associated labels
   - Error messages are properly associated with input fields
   - Color is not the only indicator of form validation state