import {
  Component, OnInit, OnDestroy, AfterViewInit,
  inject, ChangeDetectionStrategy, ChangeDetectorRef,
  ElementRef, ViewChild, NgZone, PLATFORM_ID
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ProductService }  from '../../core/services/product.service';
import { CategoryService } from '../../core/services/category.service';
import { CartService }     from '../../core/services/cart.service';
import { ToastService }    from '../../core/services/toast.service';
import { AuthService }     from '../../core/services/auth.service';
import { Product }  from '../../core/models/product.models';
import { Category } from '../../core/models/category.models';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly productService  = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly cartService     = inject(CartService);
  private readonly toast           = inject(ToastService);
  readonly auth                    = inject(AuthService);
  private readonly cdr             = inject(ChangeDetectorRef);
  private readonly zone            = inject(NgZone);
  private readonly platformId      = inject(PLATFORM_ID);
  private readonly el              = inject(ElementRef);

  @ViewChild('particleCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;

  products:   Product[]  = [];
  categories: Category[] = [];
  loading = true;

  private rafId?: number;
  private scrollHandler?: () => void;
  private mouseMoveHandler?: (e: MouseEvent) => void;
  private resizeHandler?: () => void;
  private revealObserver?: IntersectionObserver;

  ngOnInit(): void {
    this.productService.getProducts('', '', 1, 8).subscribe({
      next: result => {
        this.products = result.items ?? [];
        this.loading = false;
        this.cdr.markForCheck();

        setTimeout(() => this.observeNewRevealElements(), 80);
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
      }
    });

    this.categoryService.getCategories().subscribe({
      next: cats => { this.categories = cats ?? []; this.cdr.markForCheck(); },
      error: () => {}
    });
  }

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.zone.runOutsideAngular(() => {
      this.initParticles();
      this.initParallax();
      this.initScrollReveal();
      this.initMouseParallax();
    });
  }

  ngOnDestroy(): void {
    if (this.rafId)             cancelAnimationFrame(this.rafId);
    if (this.scrollHandler)     window.removeEventListener('scroll', this.scrollHandler);
    if (this.mouseMoveHandler)  document.removeEventListener('mousemove', this.mouseMoveHandler);
    if (this.resizeHandler)     window.removeEventListener('resize', this.resizeHandler);
    if (this.revealObserver)    this.revealObserver.disconnect();
  }

  getCatIcon(index: number): string {
    switch (index) {
      case 0:  return '⚡';
      case 1:  return '◈';
      case 2:  return '◉';
      case 3:  return '✦';
      default: return '◆';
    }
  }

  addToCart(product: Product): void {
    if (!this.auth.isLoggedIn()) {
      this.toast.warning('Please login to add items to your cart.');
      return;
    }
    this.cartService.addItem({ productId: product.id, quantity: 1 }).subscribe({
      next: () => this.toast.success(`${product.name} added to cart!`),
      error: () => this.toast.error('Could not add item to cart.')
    });
  }

  private initParticles(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const resize = () => {
      canvas.width  = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
    };
    resize();
    this.resizeHandler = resize;
    window.addEventListener('resize', resize, { passive: true });

    const count = 55;
    const pts = Array.from({ length: count }, () => ({
      x:    Math.random() * canvas.width,
      y:    Math.random() * canvas.height,
      size: Math.random() * 1.6 + 0.3,
      vx:   (Math.random() - 0.5) * 0.28,
      vy:   (Math.random() - 0.5) * 0.28,
      a:    Math.random() * 0.45 + 0.1,
      h:    Math.random() * 25
    }));

    let mx = -999, my = -999;
    this.mouseMoveHandler = (e: MouseEvent) => {
      const rect = canvas.getBoundingClientRect();
      mx = e.clientX - rect.left;
      my = e.clientY - rect.top;
    };
    document.addEventListener('mousemove', this.mouseMoveHandler, { passive: true });

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);

      for (let i = 0; i < pts.length; i++) {
        for (let j = i + 1; j < pts.length; j++) {
          const dx = pts[i].x - pts[j].x, dy = pts[i].y - pts[j].y;
          const d = Math.hypot(dx, dy);
          if (d < 105) {
            ctx.beginPath();
            ctx.strokeStyle = `rgba(232,119,34,${0.055 * (1 - d / 105)})`;
            ctx.lineWidth = 0.6;
            ctx.moveTo(pts[i].x, pts[i].y);
            ctx.lineTo(pts[j].x, pts[j].y);
            ctx.stroke();
          }
        }
      }

      pts.forEach(p => {
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(${232},${119 + p.h * 0.5},${34 + p.h * 0.4},${p.a})`;
        ctx.fill();

        const dx = p.x - mx, dy = p.y - my;
        const d = Math.hypot(dx, dy);
        if (d < 75) {
          const f = (75 - d) / 75;
          p.vx += (dx / d) * f * 0.45;
          p.vy += (dy / d) * f * 0.45;
          const spd = Math.hypot(p.vx, p.vy);
          if (spd > 2) { p.vx = (p.vx / spd) * 2; p.vy = (p.vy / spd) * 2; }
        }

        p.x += p.vx;
        p.y += p.vy;
        if (p.x < 0 || p.x > canvas.width)  p.vx *= -1;
        if (p.y < 0 || p.y > canvas.height) p.vy *= -1;
      });

      this.rafId = requestAnimationFrame(draw);
    };
    draw();
  }

  private initParallax(): void {
    const host = this.el.nativeElement as HTMLElement;
    const layers = host.querySelectorAll<HTMLElement>('[data-parallax]');

    this.scrollHandler = () => {
      layers.forEach(el => {
        const speed = parseFloat(el.dataset['parallax'] ?? '0');
        const rect  = el.parentElement!.getBoundingClientRect();
        const off   = (rect.top + rect.height / 2 - window.innerHeight / 2) * speed;
        el.style.transform = `translateY(${off}px)`;
      });
    };
    window.addEventListener('scroll', this.scrollHandler, { passive: true });
    this.scrollHandler();
  }

  private initScrollReveal(): void {
    this.revealObserver = new IntersectionObserver(entries => {
      entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('visible'); });
    }, { threshold: 0.1 });

    this.observeNewRevealElements();
  }

  observeNewRevealElements(): void {
    if (!this.revealObserver) return;
    const host = this.el.nativeElement as HTMLElement;
    host.querySelectorAll<HTMLElement>('.reveal:not(.observed)').forEach(el => {
      el.classList.add('observed');
      this.revealObserver!.observe(el);
    });
  }

  private initMouseParallax(): void {
    const host   = this.el.nativeElement as HTMLElement;
    const shapes = host.querySelectorAll<HTMLElement>('.hero-shape');

    const handler = (e: MouseEvent) => {
      const cx = (e.clientX / window.innerWidth  - 0.5) * 2;
      const cy = (e.clientY / window.innerHeight - 0.5) * 2;
      shapes.forEach((s, i) => {
        const d = (i + 1) * 11;
        s.style.transform = `translate(${cx * d}px, ${cy * d}px)`;
      });
    };

    const canvasHandler = this.mouseMoveHandler;
    this.mouseMoveHandler = (e: MouseEvent) => {
      canvasHandler?.(e);
      handler(e);
    };

    document.removeEventListener('mousemove', canvasHandler as EventListener);
    document.addEventListener('mousemove', this.mouseMoveHandler, { passive: true });
  }
}
