import { signal, computed, effect } from '@angular/core';
import type { Task } from '../models/task';


export const tasksSignal = signal<Task[]>([]);
export const selectedProjectId = signal<string | null>(null);


export const filteredTasks = computed(() =>
tasksSignal().filter(t => (selectedProjectId() ? t.projectId === selectedProjectId() : true))
);


effect(() => {
// simple persistence example
localStorage.setItem('tf_tasks', JSON.stringify(tasksSignal()));
});