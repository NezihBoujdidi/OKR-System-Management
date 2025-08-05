import { OKRSession } from "./okr-session.interface";
import { User } from "./user.interface";
import { KeyResult } from "./key-result.interface";
import { Priority } from "./Priority.enum";
import { Status } from "./Status.enum";

export interface Objective {   
    id: string;
    okrSessionId: string;
    userId: string;
    title: string;
    description: string;
    startedDate: Date;
    endDate: Date;
    status: Status;
    priority: Priority;
    responsibleTeamId: string;
    isDeleted: boolean;
    progress?: number;
}

// export enum Priority {
//     Low = 1,
//     Medium = 2,
//     High = 3,
//     Urgent = 4
//   }