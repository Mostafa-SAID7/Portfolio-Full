import { signal, WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ContactMessage, ContactReply } from '../../core/models/contact.model';
import { ContactService } from '../../core/services/contact.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog.component';
import { ToastService } from '../../shared/toast.component';
import { MessagesComponent } from './messages.component';

const message: ContactMessage = {
  id: 3, name: 'Jane', email: 'jane@example.com', subject: 'Project quote',
  message: 'Can you build a dashboard?', isRead: false, readAt: null, ipAddress: null, userAgent: null,
  createdAt: '2026-01-01T10:00:00Z', updatedAt: null
};
const storedReply: ContactReply = {
  id: 9, contactId: 3, recipientEmail: 'jane@example.com', subject: 'Re: Project quote',
  body: 'Yes — here is my availability.', sentAt: '2026-02-02T08:00:00Z'
};

describe('MessagesComponent reply composer', () => {
  let fixture: ComponentFixture<MessagesComponent>;
  let component: MessagesComponent;
  let service: {
    messages: WritableSignal<ContactMessage[]>;
    getReplies: jasmine.Spy;
    replyToMessage: jasmine.Spy;
  };
  let router: { navigate: jasmine.Spy };
  let urlParams: Record<string, string>;

  beforeEach(async () => {
    urlParams = {};
    service = {
      messages: signal([message]),
      getReplies: jasmine.createSpy('getReplies').and.returnValue(of([storedReply])),
      replyToMessage: jasmine.createSpy('replyToMessage').and.returnValue(of(storedReply)),
    };
    router = { navigate: jasmine.createSpy('navigate').and.resolveTo(true) };

    await TestBed.configureTestingModule({
      imports: [MessagesComponent],
      providers: [
        { provide: ContactService, useValue: { ...service, loading: signal(false), error: signal<string | null>(null), unreadCount: signal(1), loadMessages: jasmine.createSpy('loadMessages').and.returnValue(of([])), markAsRead: jasmine.createSpy('markAsRead').and.returnValue(of(undefined)), deleteMessage: jasmine.createSpy('deleteMessage').and.returnValue(of(undefined)) } },
        { provide: ConfirmDialogService, useValue: { confirm: jasmine.createSpy('confirm').and.resolveTo(false) } },
        { provide: ToastService, useValue: { show: jasmine.createSpy('show') } },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParamMap: { get: (key: string) => urlParams[key] ?? null } } } },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MessagesComponent);
    component = fixture.componentInstance;
    await render();
  });

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function replyButton(): HTMLButtonElement {
    return fixture.debugElement
      .queryAll(By.css('article button'))
      .map(entry => entry.nativeElement as HTMLButtonElement)
      .find(button => button.textContent?.trim() === 'Reply')!;
  }

  it('opens the composer for one message and loads that thread’s history', async () => {
    expect(fixture.debugElement.query(By.css('textarea'))).toBeNull();

    replyButton().click();
    await render();

    expect(component.openReplyFor()).toBe(3);
    expect(service.getReplies).toHaveBeenCalledOnceWith(3);
    expect(component.repliesFor(3)).toEqual([storedReply]);
    const textarea = fixture.debugElement.query(By.css('textarea')).nativeElement as HTMLTextAreaElement;
    expect(textarea.getAttribute('maxlength')).toBe('2000');
    expect(textarea.getAttribute('aria-describedby')).toBe('reply-help-3');
  });

  it('reopens the composer from ?reply= so a reload keeps the thread being answered', async () => {
    urlParams.reply = '3';
    const other = TestBed.createComponent(MessagesComponent);
    other.detectChanges();
    await other.whenStable();

    expect(other.componentInstance.openReplyFor()).toBe(3);
    expect(service.getReplies).toHaveBeenCalledWith(3);
    other.destroy();
  });

  it('sends the trimmed draft, records it and asks for nothing more', async () => {
    component.toggleReply(message);
    component.onDraftInput({ target: { value: '  Happy to help.  ' } } as unknown as Event);
    await render();

    component.sendReply(message);
    await render();

    expect(service.replyToMessage).toHaveBeenCalledOnceWith(3, 'Happy to help.');
    expect(component.replyDraft()).toBe('');
    expect(component.repliesFor(3)[0]).toEqual(storedReply);
    expect(component.replyError()).toBeNull();
    expect(component.replyDelivered()).toBeTrue();
    expect(
      fixture.debugElement
        .queryAll(By.css('[role="status"]'))
        .some(entry => (entry.nativeElement as HTMLElement).textContent?.includes('Reply delivered'))
    ).toBeTrue();
  });

  it('keeps the draft and shows the server reason when delivery fails', async () => {
    service.replyToMessage.and.returnValue(
      throwError(() => ({ error: { message: 'The reply could not be sent — the mail server did not answer.' } })),
    );
    component.toggleReply(message);
    component.onDraftInput({ target: { value: 'Happy to help.' } } as unknown as Event);

    component.sendReply(message);
    await render();

    expect(component.replyError()).toContain('mail server did not answer');
    expect(component.replyDraft()).toBe('Happy to help.');
    expect(component.replyDelivered()).toBeFalse();
    // Only the previously loaded history remains — a failed send records nothing.
    expect(component.repliesFor(3)).toEqual([storedReply]);
    expect(fixture.debugElement.query(By.css('[role="alert"]'))).not.toBeNull();
  });

  it('will not send an empty draft', async () => {
    component.toggleReply(message);
    await render();

    component.sendReply(message);

    expect(service.replyToMessage).not.toHaveBeenCalled();
    const send = fixture.debugElement
      .queryAll(By.css('article button'))
      .map(entry => entry.nativeElement as HTMLButtonElement)
      .find(button => button.textContent?.trim() === 'Send reply')!;
    expect(send.disabled).toBeTrue();
  });

  it('offers a mailto fallback that threads the subject once', async () => {
    component.toggleReply(message);
    component.onDraftInput({ target: { value: 'Happy to help.' } } as unknown as Event);

    const href = decodeURIComponent(component.mailtoHref({ ...message, subject: 'Re: Project quote' }));
    expect(href).toContain('mailto:jane@example.com?');
    expect(href).toContain('subject=Re: Project quote');
    expect(href).toContain('body=Happy to help.');

    const threaded = decodeURIComponent(component.mailtoHref(message));
    expect(threaded).toContain('subject=Re: Project quote');
  });

  it('mirrors the open composer into the URL and clears it on cancel', async () => {
    component.toggleReply(message);
    expectLastQuery({ reply: 3 });

    component.closeReply();
    expect(component.openReplyFor()).toBeNull();
    expectLastQuery({ reply: null });
  });

  function expectLastQuery(partial: Record<string, unknown>): void {
    const args = router.navigate.calls.mostRecent().args as [unknown, { queryParams: Record<string, unknown> }];
    expect(args[1].queryParams).toEqual(jasmine.objectContaining(partial));
  }
});
