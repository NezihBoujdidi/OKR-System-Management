export interface Organization {
    id: string;
    name: string;
    description?: string;
    country: string;
    industry: string;
    size: number;
    email: string;
    phone: string;
    isActive: boolean;
    createdDate?: string;
    modifiedDate?: string;
    subscriptionPlan?: string;
  }
  
export interface CreateOrganizationCommand {
  name: string;
  description?: string;
  country?: string;
  industry?: string;
  email?: string;
  phone?: string;
  size?: number;
  isActive: boolean;
}

export interface UpdateOrganizationCommand {
  name: string;
  description?: string;
  country?: string;
  industry?: string;
  email?: string;
  phone?: string;
  size?: number;
  isActive?: boolean;
}
  
export interface CreateOrganizationResponse {
  id: string;
  message: string;
}

// Interface for paginated API responses
export interface PaginatedListResult<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
  