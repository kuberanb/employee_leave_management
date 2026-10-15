import { AfterViewInit, Directive, ElementRef, HostListener, inject, OnDestroy } from '@angular/core';

@Directive({ selector: '[appDialog]' })
export class DialogDirective implements AfterViewInit, OnDestroy {
  private element = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private previous = document.activeElement as HTMLElement | null;
  private overflow = document.body.style.overflow;
  private focusable() {
    return Array.from(this.element.querySelectorAll<HTMLElement>('button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [tabindex="0"]'));
  }
  ngAfterViewInit() {
    document.body.style.overflow = 'hidden';
    this.focusable()[0]?.focus();
  }
  @HostListener('keydown', ['$event'])
  trap(event: KeyboardEvent) {
    if (event.key !== 'Tab') return;
    const elements = this.focusable();
    const first = elements[0]; const last = elements[elements.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
  }
  ngOnDestroy() { document.body.style.overflow = this.overflow; this.previous?.focus(); }
}
