export interface TableColumn {
  key: string;
  label: string;
  type?: 'text' | 'badge' | 'email' | 'user' | 'image' | 'actions' | 'organization' | 'subscription';
  badgeType?: 'status' | 'role';
  userConfig?: {
    nameField: string;
    emailField: string;
    imageField: string;
  };
  width?: string;
  sortable?: boolean;
}

export interface TableConfig {
  columns: TableColumn[];
  data: any[];
  itemsPerPage: number;
  enablePagination: boolean;
  source?: 'employees' | 'users' | 'organization';
}