import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent, ConfirmDialogService } from './confirm-dialog.component';

// These tests use the browser's real HTMLDialogElement, not a focus-trap mock.
describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let service: ConfirmDialogService;
  let opener: HTMLButtonElement;
  let dialog: HTMLDialogElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ConfirmDialogComponent] }).compileComponents();
    opener = document.createElement('button');
    opener.textContent = 'Open confirmation';
    document.body.appendChild(opener);
    opener.focus();
    fixture = TestBed.createComponent(ConfirmDialogComponent);
    service = TestBed.inject(ConfirmDialogService);
    fixture.detectChanges();
    dialog = fixture.nativeElement.querySelector('dialog');
  });

  afterEach(() => {
    if (!fixture.componentRef.hostView.destroyed) fixture.destroy();
    opener.remove();
  });

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('opens natively with cancel focused and prevents focus escaping the modal', async () => {
    const result = service.confirm({ title: 'Delete item?', message: 'This cannot be undone.' });
    await render();
    expect(dialog.open).toBeTrue();
    expect(dialog.matches(':modal')).toBeTrue();
    const cancel = dialog.querySelector('.btn-ghost') as HTMLButtonElement;
    expect(document.activeElement).toBe(cancel);
    opener.focus();
    expect(document.activeElement).toBe(cancel);
    cancel.click();
    await render();
    expect(await result).toBeFalse();
    expect(dialog.open).toBeFalse();
    expect(document.activeElement).toBe(opener);
  });

  it('cancels through the native cancel event and restores focus', async () => {
    const result = service.confirm({ title: 'Delete?', message: 'Cancel safely.' });
    await render();
    const event = new Event('cancel', { cancelable: true });
    dialog.dispatchEvent(event);
    expect(event.defaultPrevented).toBeTrue();
    await render();
    expect(await result).toBeFalse();
    expect(dialog.open).toBeFalse();
    expect(document.activeElement).toBe(opener);
  });

  it('cancels backdrop clicks but not dialog content clicks', async () => {
    const result = service.confirm({ title: 'Delete?', message: 'Click outside to cancel.' });
    await render();
    (dialog.querySelector('p') as HTMLElement).click();
    expect(service.request()).not.toBeNull();
    const rect = dialog.getBoundingClientRect();
    dialog.dispatchEvent(new MouseEvent('click', { clientX: rect.left - 1, clientY: rect.top - 1 }));
    await render();
    expect(await result).toBeFalse();
  });

  it('settles explicit confirmation true and a replaced request false', async () => {
    const previous = service.confirm({ title: 'First', message: 'Old request' });
    await render();
    const previousId = service.request()!.id;
    const current = service.confirm({ title: 'Second', message: 'New request' });
    await render();
    expect(await previous).toBeFalse();
    expect(service.request()!.id).not.toBe(previousId);
    expect(document.activeElement).toBe(dialog.querySelector('.btn-ghost'));
    (dialog.querySelector('.btn-danger') as HTMLButtonElement).click();
    await render();
    expect(await current).toBeTrue();
    expect(document.activeElement).toBe(opener);
  });

  it('cancels if closed natively without an explicit answer', async () => {
    const result = service.confirm({ title: 'Delete?', message: 'Native close' });
    await render();
    dialog.close();
    dialog.dispatchEvent(new Event('close'));
    await render();
    expect(await result).toBeFalse();
  });

  it('settles pending work false, closes, and restores focus on destruction', async () => {
    const result = service.confirm({ title: 'Delete?', message: 'Unmounting cancels.' });
    await render();
    fixture.destroy();
    expect(await result).toBeFalse();
    expect(service.request()).toBeNull();
    expect(dialog.open).toBeFalse();
    expect(document.activeElement).toBe(opener);
  });
});
