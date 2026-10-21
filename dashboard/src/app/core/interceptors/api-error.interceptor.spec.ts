import { HttpClient, HttpErrorResponse, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../../shared/toast.component';
import { apiErrorInterceptor, normalizeApiError } from './api-error.interceptor';

describe('API error normalization', () => {
  it('retains domain field validation', () => {
    expect(normalizeApiError(new HttpErrorResponse({ status: 400, error: { message: 'Invalid', errors: { TitleAr: ['Required'] } } })))
      .toEqual({ message: 'Invalid', errors: { TitleAr: ['Required'] } });
  });

  it('understands ProblemDetails and rejects non-string validation entries', () => {
    const result = normalizeApiError(new HttpErrorResponse({ status: 400, error: { title: 'Invalid request', detail: 'Please correct the fields.', errors: { CompanyAr: ['Required', 4] } } }));
    expect(result.message).toBe('Please correct the fields.');
    expect(result.errors).toEqual({ CompanyAr: ['Required'] });
  });

  it('supports legacy FluentValidation failures', () => {
    expect(normalizeApiError(new HttpErrorResponse({ status: 400, error: [{ propertyName: 'NameAr', errorMessage: 'Required' }] })))
      .toEqual({ message: 'Required', errors: { NameAr: ['Required'] } });
  });

  it('provides a network hint without rendering arbitrary objects', () => {
    expect(normalizeApiError(new HttpErrorResponse({ status: 0 })).message).toContain('Cannot reach');
    expect(normalizeApiError(new HttpErrorResponse({ status: 500, error: { message: {} } })).message).toContain('Request failed');
  });
});

describe('apiErrorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    auth = jasmine.createSpyObj('AuthService', ['handleAuthFailure']);
    toast = jasmine.createSpyObj('ToastService', ['show']);
    TestBed.configureTestingModule({ providers: [provideHttpClient(withInterceptors([apiErrorInterceptor])), provideHttpClientTesting(), { provide: AuthService, useValue: auth }, { provide: ToastService, useValue: toast }] });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });
  afterEach(() => controller.verify());

  it('preserves headers and errors for a rate-limited response', () => {
    let received: HttpErrorResponse | undefined;
    http.get('/api/v1/Projects').subscribe({ error: error => received = error });
    controller.expectOne('/api/v1/Projects').flush({ message: 'Wait', errors: { TitleEn: ['Required'] } }, { status: 429, statusText: 'Too Many Requests', headers: { 'Retry-After': '60' } });
    expect(received?.headers.get('Retry-After')).toBe('60');
    expect(received?.error.errors.TitleEn).toEqual(['Required']);
  });

  it('does not treat rejected login credentials as an expired session', () => {
    http.post('/api/v1/auth/login', {}).subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/auth/login').flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });
    expect(auth.handleAuthFailure).not.toHaveBeenCalled();
  });

  it('clears an expired authenticated session and surfaces forbidden actions', () => {
    http.get('/api/v1/Projects').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/Projects').flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(auth.handleAuthFailure).toHaveBeenCalledTimes(1);
    http.get('/api/v1/Skills').subscribe({ error: () => undefined });
    controller.expectOne('/api/v1/Skills').flush({ message: 'Not permitted' }, { status: 403, statusText: 'Forbidden' });
    expect(toast.show).toHaveBeenCalledWith('Not permitted', 'error');
  });
});
