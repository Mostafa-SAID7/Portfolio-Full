import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { ApiService } from '../../../core/services/api.service';
import { ApiResponse } from '../../../core/models/api-response.model';

export interface ContactMessage {
  id?: number;
  name: string;
  email: string;
  subject: string;
  message: string;
  createdAt?: Date;
}

@Injectable({
  providedIn: 'root'
})
export class ContactService {
  constructor(private apiService: ApiService) {}

  sendMessage(contactData: Omit<ContactMessage, 'id' | 'createdAt'>): Observable<ApiResponse<ContactMessage>> {
    // In a real app, this would call the API
    // return this.apiService.post<ApiResponse<ContactMessage>>('contact', contactData);

    // Mock implementation - simulate API delay
    const mockResponse: ContactMessage = {
      ...contactData,
      id: Math.floor(Math.random() * 1000),
      createdAt: new Date()
    };

    return of({
      success: true,
      data: mockResponse,
      message: 'Message sent successfully! I\'ll get back to you soon.'
    });
  }

  getMessages(): Observable<ApiResponse<ContactMessage[]>> {
    // In a real app: return this.apiService.get<ApiResponse<ContactMessage[]>>('contact/messages');

    // Mock data for admin panel
    const mockMessages: ContactMessage[] = [
      {
        id: 1,
        name: 'John Doe',
        email: 'john@example.com',
        subject: 'Project Inquiry',
        message: 'Hi, I\'m interested in working with you on a new project.',
        createdAt: new Date('2023-12-01')
      },
      {
        id: 2,
        name: 'Jane Smith',
        email: 'jane@example.com',
        subject: 'Collaboration Opportunity',
        message: 'I\'d like to discuss a potential collaboration.',
        createdAt: new Date('2023-12-02')
      }
    ];

    return of({
      success: true,
      data: mockMessages,
      message: 'Messages retrieved successfully'
    });
  }
}