export interface KeyMetric {
  label: string;
  value: string | number;
  trend: string;
  isPositive: boolean;
}

export interface Activity {
  title: string;
  description: string;
  time: string;
  type?: 'new_employee' | 'project' | 'meeting' | 'performance';
}

export interface ChartData {
  labels: string[];
  datasets: {
    label: string;
    data: number[];
    backgroundColor?: string | string[];
    borderColor?: string;
    borderWidth?: number;
    fill?: boolean;
  }[];
}

export interface TeamPerformance {
  team: string;
  progress: number;
  total: number;
  status: 'on-track' | 'at-risk' | 'behind';
}

export interface OKRProgress {
  objective: string;
  progress: number;
  dueDate: string;
  status: 'on-track' | 'at-risk' | 'behind';
}

export interface TrendData {
  current: number;
  previous: number;
  percentageChange: number;
}

export interface OKRMetrics extends KeyMetric {
  trendData: TrendData;
}

export interface DepartmentProgress {
  department: string;
  objectives: number;
  completed: number;
  inProgress: number;
  atRisk: number;
  progress: number;
  trend: number;
}

export interface TimelineItem {
  milestone: string;
  date: string;
  status: 'completed' | 'in-progress' | 'upcoming';
  owner: string;
}

export interface RiskMetric {
  category: string;
  count: number;
  trend: number;
  priority: 'high' | 'medium' | 'low';
}

export interface RawOKR {
  id: number;
  title: string;
  description: string;
  targetDate: string;
  status: string;
  progress: number;
  department: string;
  owner: string;
  keyResults: RawKeyResult[];
}

export interface RawKeyResult {
  id: number;
  metric: string;
  target: number;
  current: number;
  startValue: number;
}

export interface RawDepartment {
  id: number;
  name: string;
  okrs: RawOKR[];
  employeeCount: number;
} 