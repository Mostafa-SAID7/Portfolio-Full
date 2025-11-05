import { Component, inject, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectFilter } from '../../../core/models/project.model';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { InputComponent } from '../../../shared/components/ui/input/input.component';

@Component({
  selector: 'app-project-filter',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, InputComponent],
  template: `
    <div class="project-filter">
      <div class="filter-controls">
        <app-input
          label="Search Projects"
          placeholder="Search by title, description, or technology..."
          [(ngModel)]="filter.search"
          (ngModelChange)="onFilterChange()"
        ></app-input>

        <div class="filter-row">
          <div class="filter-group">
            <label class="filter-label">Technology</label>
            <select
              class="filter-select"
              [(ngModel)]="filter.technology"
              (ngModelChange)="onFilterChange()"
            >
              <option value="">All Technologies</option>
              <option value="React">React</option>
              <option value="Angular">Angular</option>
              <option value="Vue.js">Vue.js</option>
              <option value="Node.js">Node.js</option>
              <option value="Python">Python</option>
              <option value="TypeScript">TypeScript</option>
              <option value="JavaScript">JavaScript</option>
            </select>
          </div>

          <div class="filter-group">
            <label class="filter-label">Show Only</label>
            <div class="filter-options">
              <label class="checkbox-label">
                <input
                  type="checkbox"
                  [(ngModel)]="filter.featured"
                  (ngModelChange)="onFilterChange()"
                />
                Featured Projects
              </label>
            </div>
          </div>
        </div>

        <div class="filter-actions">
          <app-button variant="outline" (click)="clearFilters()">
            Clear Filters
          </app-button>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./project-filter.component.scss']
})
export class ProjectFilterComponent {
  @Output() filterChange = new EventEmitter<ProjectFilter>();

  filter: ProjectFilter = {
    search: '',
    technology: '',
    featured: false
  };

  onFilterChange(): void {
    this.filterChange.emit(this.filter);
  }

  clearFilters(): void {
    this.filter = {
      search: '',
      technology: '',
      featured: false
    };
    this.filterChange.emit(this.filter);
  }
}