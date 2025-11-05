import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ContactService, ContactMessage } from '../services/contact.service';
import { ButtonComponent } from '../../../shared/components/ui/button/button.component';
import { InputComponent } from '../../../shared/components/ui/input/input.component';
import { CardComponent } from '../../../shared/components/ui/card/card.component';
import { ToastService } from '../../../core/services/toast.service';
import { LoadingService } from '../../../core/services/loading.service';
import { emailValidator } from '../../../shared/utils/validators';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonComponent, InputComponent, CardComponent],
  template: `
    <app-card class="contact-form-card">
      <div card-header>
        <h2>Send a Message</h2>
      </div>
      <form [formGroup]="contactForm" (ngSubmit)="onSubmit()" class="contact-form">
        <div class="form-row">
          <app-input
            label="Name"
            placeholder="Your full name"
            formControlName="name"
            [error]="getErrorMessage('name')"
          ></app-input>

          <app-input
            label="Email"
            type="email"
            placeholder="your.email@example.com"
            formControlName="email"
            [error]="getErrorMessage('email')"
          ></app-input>
        </div>

        <app-input
          label="Subject"
          placeholder="What's this about?"
          formControlName="subject"
          [error]="getErrorMessage('subject')"
        ></app-input>

        <div class="form-group">
          <label class="form-label">Message</label>
          <textarea
            class="form-textarea"
            placeholder="Tell me about your project or just say hello..."
            formControlName="message"
            rows="6"
            [class.error]="contactForm.get('message')?.invalid && contactForm.get('message')?.touched"
          ></textarea>
          <div class="form-error" *ngIf="contactForm.get('message')?.invalid && contactForm.get('message')?.touched">
            {{ getErrorMessage('message') }}
          </div>
        </div>

        <div class="form-actions">
          <app-button
            type="submit"
            variant="primary"
            size="lg"
            [disabled]="contactForm.invalid || isSubmitting"
          >
            {{ isSubmitting ? 'Sending...' : 'Send Message' }}
          </app-button>
        </div>
      </form>
    </app-card>
  `,
  styleUrls: ['./contact-form.component.scss']
})
export class ContactFormComponent {
  private fb = inject(FormBuilder);
  private contactService = inject(ContactService);
  private toastService = inject(ToastService);
  private loadingService = inject(LoadingService);

  contactForm: FormGroup;
  isSubmitting = false;

  constructor() {
    this.contactForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, emailValidator()]],
      subject: ['', [Validators.required, Validators.minLength(5)]],
      message: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  onSubmit(): void {
    if (this.contactForm.valid) {
      this.isSubmitting = true;
      this.loadingService.show();

      const formData: Omit<ContactMessage, 'id' | 'createdAt'> = this.contactForm.value;

      this.contactService.sendMessage(formData).subscribe({
        next: (response) => {
          this.toastService.show(response.message || 'Message sent successfully!', 'success');
          this.contactForm.reset();
        },
        error: (error) => {
          this.toastService.show('Failed to send message. Please try again.', 'error');
        },
        complete: () => {
          this.isSubmitting = false;
          this.loadingService.hide();
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  getErrorMessage(fieldName: string): string {
    const control = this.contactForm.get(fieldName);
    if (control?.errors && control.touched) {
      if (control.errors['required']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} is required`;
      }
      if (control.errors['minlength']) {
        return `${fieldName.charAt(0).toUpperCase() + fieldName.slice(1)} must be at least ${control.errors['minlength'].requiredLength} characters`;
      }
      if (control.errors['invalidEmail']) {
        return 'Please enter a valid email address';
      }
    }
    return '';
  }

  private markFormGroupTouched(): void {
    Object.keys(this.contactForm.controls).forEach(key => {
      const control = this.contactForm.get(key);
      control?.markAsTouched();
    });
  }
}