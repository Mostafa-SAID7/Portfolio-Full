import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { API_URL } from '../api.config';
import { ContactMessage, ContactReply } from '../models/contact.model';
import { ContactService } from './contact.service';

const message: ContactMessage = {
  id: 3, name: 'Jane', email: 'jane@example.com', subject: 'Project quote',
  message: 'Can you help?', isRead: false, readAt: null, ipAddress: null, userAgent: null,
  createdAt: '2026-01-01T10:00:00Z', updatedAt: null
};
const reply: ContactReply = {
  id: 9, contactId: 3, recipientEmail: 'jane@example.com', subject: 'Re: Project quote',
  body: 'Happy to help.', sentAt: '2026-02-02T08:00:00Z'
};

describe('ContactService reply endpoints', () => {
  let service: ContactService;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ContactService);
    controller = TestBed.inject(HttpTestingController);
    service.loadMessages({ force: true }).subscribe();
    controller.expectOne(`${API_URL}/Contact`).flush([message]);
  });

  afterEach(() => controller.verify());

  it('reads one thread’s history from the replies endpoint', async () => {
    const promise = firstValueFrom(service.getReplies(3));

    controller.expectOne(`${API_URL}/Contact/3/replies`).flush([reply]);

    expect(await promise).toEqual([reply]);
  });

  it('posts the draft and marks the message read with the delivery timestamp', async () => {
    const promise = firstValueFrom(service.replyToMessage(3, 'Happy to help.'));
    const request = controller.expectOne(`${API_URL}/Contact/3/reply`);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ body: 'Happy to help.' });
    request.flush(reply);

    expect(await promise).toEqual(reply);
    expect(service.messages()[0].isRead).toBeTrue();
    expect(service.messages()[0].readAt).toBe('2026-02-02T08:00:00Z');
  });

  it('leaves the inbox unchanged when the reply was not delivered', async () => {
    const promise = firstValueFrom(service.replyToMessage(3, 'Happy to help.'));

    controller.expectOne(`${API_URL}/Contact/3/reply`)
      .flush({ message: 'The reply could not be sent.' }, { status: 503, statusText: 'Unavailable' });

    await expectAsync(promise).toBeRejected();
    expect(service.messages()[0].isRead).toBeFalse();
    expect(service.messages()[0].readAt).toBeNull();
  });
});
