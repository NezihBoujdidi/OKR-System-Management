export interface SidebarSession {
  id: string;
  title: string;
  period: string;
  status: string;
  color: string;
  owner: {
    name: string;
    avatar: string;
  };
}

export interface SidebarItem {
  id: string;
  title: string;
  icon: string;
  route?: string;
  sessions?: SidebarSession[];
  isActive?: boolean;
} 