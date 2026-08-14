export type BackgroundJobKind = 'Immediate' | 'Delayed' | 'Scheduled' | 'Recurring';

export type BackgroundJobStatus =
  | 'Pending'
  | 'Scheduled'
  | 'Running'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'DeadLetter';

export interface BackgroundJobSummary {
  id: number;
  jobType: string;
  kind: BackgroundJobKind;
  status: BackgroundJobStatus;
  priority: number;
  executeAtUtc: string;
  attemptCount: number;
  maxAttempts: number;
  lastError: string | null;
  nextRetryAtUtc: string | null;
  createdAtUtc: string;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
  recurringScheduleKey: string | null;
}

export interface BackgroundJobExecution {
  id: number;
  attemptNumber: number;
  status: 'Running' | 'Completed' | 'Failed' | 'Cancelled';
  startedAtUtc: string;
  completedAtUtc: string | null;
  errorMessage: string | null;
}

export interface BackgroundJobDetail extends BackgroundJobSummary {
  payloadJson: string | null;
  idempotencyKey: string | null;
  updatedAtUtc: string;
  cancelledAtUtc: string | null;
  executions: BackgroundJobExecution[];
}

export interface RecurringJobScheduleSummary {
  id: number;
  scheduleKey: string;
  jobType: string;
  intervalSeconds: number;
  maxAttempts: number;
  isEnabled: boolean;
  nextRunAtUtc: string;
  lastRunAtUtc: string | null;
}
