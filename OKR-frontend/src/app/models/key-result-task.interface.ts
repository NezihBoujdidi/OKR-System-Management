import { Priority } from "./Priority.enum";
import { Status } from "./Status.enum";

export interface KeyResultTask {
  id: string;
  keyResultId: string;
  userId: string;
  title: string;
  description?: string;
  startedDate: Date;
  endDate: Date;
  createdDate?: Date;
  modifiedDate?: Date;
  collaboratorId: string;
  progress: number;
  priority: Priority;
  status: Status;
  isDeleted: boolean;
}
