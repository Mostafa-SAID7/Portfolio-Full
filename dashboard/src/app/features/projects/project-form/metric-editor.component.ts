import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

/** One metric row: a label (bilingual) and its value. */
export interface MetricRow {
  labelEn: string;
  labelAr: string;
  value: string;
}

@Component({
  selector: 'app-metric-editor',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './metric-editor.component.html'
})
export class MetricEditorComponent {
  @Input() label = 'Metrics';
  @Input() rows: MetricRow[] = [];
  @Input() max = 6;
  @Input() arabic = false;
  @Output() rowsChange = new EventEmitter<MetricRow[]>();

  add(): void {
    if (this.rows.length >= this.max) return;
    this.rows = [...this.rows, { labelEn: '', labelAr: '', value: '' }];
    this.rowsChange.emit(this.rows);
  }

  remove(index: number): void {
    this.rows = this.rows.filter((_, position) => position !== index);
    this.rowsChange.emit(this.rows);
  }

  update(index: number, field: keyof MetricRow, value: string): void {
    this.rows = this.rows.map((row, position) => (position === index ? { ...row, [field]: value } : row));
    this.rowsChange.emit(this.rows);
  }
}
