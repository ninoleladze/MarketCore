import { Component, AfterViewInit, OnDestroy, NgZone, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { ToastNotificationComponent } from './shared/components/toast-notification/toast-notification.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ToastNotificationComponent],
  template: `
    <div id="cursor-dot"></div>
    <div id="cursor-ring"></div>
    <app-navbar />
    <main class="main-content">
      <router-outlet />
    </main>
    <app-footer />
    <app-toast-notification />
  `,
  styles: [`
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .main-content { flex: 1; }
  `]
})
export class App implements AfterViewInit, OnDestroy {
  private readonly zone = inject(NgZone);

  private mouseX = 0;
  private mouseY = 0;
  private ringX  = 0;
  private ringY  = 0;
  private rafId?: number;

  private dot!: HTMLElement;
  private ring!: HTMLElement;

  private onMouseMove = (e: MouseEvent) => {
    this.mouseX = e.clientX;
    this.mouseY = e.clientY;
    this.dot.style.left = this.mouseX + 'px';
    this.dot.style.top  = this.mouseY + 'px';
  };

  ngAfterViewInit(): void {
    this.dot  = document.getElementById('cursor-dot')!;
    this.ring = document.getElementById('cursor-ring')!;
    if (!this.dot || !this.ring) return;

    this.zone.runOutsideAngular(() => {
      document.addEventListener('mousemove', this.onMouseMove, { passive: true });

      const lerp = (a: number, b: number, t: number) => a + (b - a) * t;

      const animate = () => {
        this.ringX = lerp(this.ringX, this.mouseX, 0.1);
        this.ringY = lerp(this.ringY, this.mouseY, 0.1);
        this.ring.style.left = this.ringX + 'px';
        this.ring.style.top  = this.ringY + 'px';
        this.rafId = requestAnimationFrame(animate);
      };
      animate();
    });

    const hoverTargets = 'a, button, input, select, textarea, [role="button"]';
    document.querySelectorAll<HTMLElement>(hoverTargets).forEach(el => {
      el.addEventListener('mouseenter', () => document.body.classList.add('cursor-hover'));
      el.addEventListener('mouseleave', () => document.body.classList.remove('cursor-hover'));
    });

    const observer = new MutationObserver(() => {
      document.querySelectorAll<HTMLElement>(hoverTargets).forEach(el => {
        if ((el as any).__cursorBound) return;
        (el as any).__cursorBound = true;
        el.addEventListener('mouseenter', () => document.body.classList.add('cursor-hover'));
        el.addEventListener('mouseleave', () => document.body.classList.remove('cursor-hover'));
      });
    });
    observer.observe(document.body, { childList: true, subtree: true });
  }

  ngOnDestroy(): void {
    document.removeEventListener('mousemove', this.onMouseMove);
    if (this.rafId) cancelAnimationFrame(this.rafId);
  }
}
