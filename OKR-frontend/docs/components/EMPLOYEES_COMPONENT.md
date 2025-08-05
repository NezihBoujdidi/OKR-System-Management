# Employees Component Documentation

## Overview

The Employees Component provides a comprehensive interface for managing organization members within the NXM Tensai OKR Management System. It allows administrators and managers to view, filter, sort, invite, and manage employees across the organization with different permission levels based on user roles.

## Component Structure

### Location
- **Module**: `EmployeesModule` 
- **Component**: `EmployeesComponent`
- **Path**: `src/app/features/employees/components/employees.component.ts`

### Dependencies

- **Core Services**:
  - `UserService` - Handles user data fetching and management
  - `TeamService` - Retrieves team relationships for employees
  - `AuthStateService` - Provides current user and permission information
  - `AuthService` - Manages invitation processes

- **Angular Features**:
  - `ReactiveFormsModule` & `FormsModule` - For form handling
  - `HostListener` - For detecting clicks outside dropdowns
  - Templating with `ng-template` - For custom table actions

- **Shared Components**:
  - Table and Grid components for different display modes
  - Filter and sort components for data manipulation

## Features

### 1. Employee Management Dashboard

The component offers a complete employee management solution:
- List and grid views of employees
- Advanced filtering and sorting functionality
- Role-based actions and permissions
- Employee invitation system
- Employee information display

### 2. Dual View Modes

Users can toggle between two different view modes:
- **List View**: Tabular format with detailed information columns
- **Grid View**: Card-based layout with visual focus on profile pictures

### 3. Advanced Filtering

The component provides robust filtering capabilities:
- Filter by roles (TeamManager, Collaborator, etc.)
- Filter by positions (job titles)
- Filter by status (Enabled, Disabled)
- Filter combinations with multiple criteria
- Instant updating of displayed results

### 4. Dynamic Sorting

Users can sort employee data by:
- Name (First and Last)
- Email
- Role
- Position
- Toggle between ascending and descending order

### 5. Search Functionality

Robust search functionality allows users to:
- Search across multiple employee fields simultaneously
- See instant results as they type
- Combine search with existing filters

### 6. User Invitation System

Organization admins can invite new users:
- Send invitation emails
- Assign default roles
- Integrate with the organization's user hierarchy

### 7. Employee Actions

Contextual actions for employee management:
- View detailed employee information
- For Organization Admins:
  - Disable (remove) employees from organization
  - Re-enable previously disabled employees

### 8. Responsive Design

The interface adapts to different screen sizes:
- Optimized layouts for desktop and tablet
- Responsive table and grid components
- Mobile-friendly action menus and modals

## Data Structure

The component works with the following primary data structures:

### User/Employee Object

Core employee data includes:
- Basic information (name, email, profile picture)
- Organizational role (TeamManager, Collaborator)
- Position (job title)
- Status (enabled/disabled)
- Dates (join date, birth date)
- Contact information

### Table and Grid Configurations

The component uses configuration objects to define display settings:
- Column definitions and formatting
- Data source and transformation rules
- Pagination settings
- Custom templates for special fields

## State Management

Key component state variables:

- `employees`: The complete list of all employees
- `filteredEmployees`: Currently displayed employees after filtering/sorting
- `selectedView`: Current view mode ('list' or 'grid')
- `currentSort`: Active sorting configuration
- `activeFilters`: Currently applied filter settings
- `showMemberDetailsDrawer`: Controls the detail panel visibility
- `selectedEmployee`: Currently selected employee for actions

## User Interface Elements

### Main Interface Components:
- **Top Bar**: Search, invite users button
- **Filter Bar**: View toggles, filter & sort dropdowns
- **Content Area**: List or grid display of employees
- **Action Menus**: Contextual employee actions
- **Details Panel**: Slide-in panel for employee details

### Modal Components:
- **Invite Modal**: Form for inviting new users
- **Delete Confirmation**: Modal for confirming employee removal

## Usage

### Routing

The component is accessible via the following route:
- `/employees` - Main employee management interface

### Permissions

Access and capabilities are role-based:
- **Organization Admin**: Full access to all features including invites, employee disabling/enabling
- **Other Roles**: Can view employee information but with limited actions

## User Interactions

### Viewing Employees

1. Toggle between list and grid views using the view buttons
2. Paginate through employees using the built-in pagination controls
3. Click on an employee or the details action to view more information

### Filtering & Sorting

1. Click the filter button to open the filter panel
2. Select filter criteria across different categories
3. Click the sort button to access sorting options
4. Select sort field and direction

### Inviting Users

1. Click "Invite users" button (Organization Admin only)
2. Enter email address and select role
3. Send invitation through the modal form

### Employee Management

1. Access employee actions through the three dots menu
2. View full employee details in the slide-in panel
3. Disable/enable employees as needed (Organization Admin only)

## Custom Methods

| Method | Description |
|--------|-------------|
| `loadOrganizationEmployees()` | Fetches employees for the current organization |
| `toggleView()` | Switches between list and grid views |
| `searchEmployees()` | Filters employees based on search term |
| `applyFilters()` | Applies selected filters to employee list |
| `sortEmployees()` | Sorts employee list based on selected criteria |
| `inviteUser()` | Opens invitation modal |
| `submitInvites()` | Processes invitation requests |
| `showEmployeeDetails()` | Displays the employee details panel |
| `enableEmployee()` / `deleteEmployee()` | Manages employee status |

## Error Handling

- Validation for email addresses in invitation form
- Error handling for API requests
- Defensive programming for missing or incomplete data

## Best Practices & Notes

1. **Permission Management**:
   - Actions are dynamically shown/hidden based on user roles
   - Sensitive information is only displayed to appropriate roles

2. **Performance Considerations**:
   - Filtering and sorting happen client-side for immediate feedback
   - Pagination helps manage large employee lists efficiently

3. **UX Patterns**:
   - Consistent confirmation flows for important actions
   - Progressive disclosure of information
   - Contextual actions presented at the point of need

4. **Accessibility Features**:
   - Proper focus management
   - Semantic HTML structure
   - Screenreader-friendly text alternatives

5. **State Management**:
   - Clear separation between full data and filtered views
   - Consistent handling of selected items
   - Modal and drawer states properly managed

6. **Integration Points**:
   - Connects with user authentication and authorization system
   - Integrates with team management for employee relationships
   - Works with invitation system for adding new users