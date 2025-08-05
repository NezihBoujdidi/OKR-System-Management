import { KeyResultTask } from './key-result-task.interface';
import { Status } from './Status.enum';

export interface KeyResult {
  id: string;
  objectiveId: string;
  userId: string;
  title: string;
  description?: string;
  startDate: Date;
  endDate: Date;
  // priority?:any;
  // targetValue?: number; //exemple 20% growth - 100 total sales ...
  // currentValue?: number;
  progress: number;
  status?: Status;
  isDeleted: boolean;
}

// public Guid ObjectiveId { get; set; }
// public Objective Objective { get; set; } = null!;
// public Guid UserId { get; set; }
// public User Owner { get; set; } = null!;
// public bool IsDeleted { get; set; } = false;

