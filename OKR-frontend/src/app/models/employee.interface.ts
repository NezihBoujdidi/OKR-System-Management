export interface Employee {
  id: number;
  name: string;
  email: string;
  team: string;
  role: string;
  imageUrl: string;
}

export interface BulkEditData {
  selectedEmployees: string[];
  newTeam: string | null;
  newRole: string | null;
}

export interface FilterOptions {
  teams: string[];
  roles: string[];
  positions: string[];
  status: string[];
}

export interface SortOption {
  value: string;
  label: string;
}
