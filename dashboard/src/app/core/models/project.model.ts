export interface Project {
  id: string;
  name: string;
  description: string;
  color: string;
  createdAt: Date;
  updatedAt: Date;
  ownerId: string;
  teamIds: string[];
}

export interface CreateProjectDto {
  name: string;
  description: string;
  color: string;
  teamIds: string[];
}

// src/app/core/models/task.model.ts
export type TaskStatus = 'todo' | 'in-progress' | 'review' | 'done';
export type TaskPriority = 'low' | 'medium' | 'high' | 'urgent';

export interface Task {
  id: string;
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  projectId: string;
  assigneeId?: string;
  dueDate?: Date;
  tags: string[];
  order: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateTaskDto {
  title: string;
  description: string;
  status: TaskStatus;
  priority: TaskPriority;
  projectId: string;
  assigneeId?: string;
  dueDate?: Date;
  tags: string[];
}

export interface UpdateTaskDto extends Partial<CreateTaskDto> {
  order?: number;
}
