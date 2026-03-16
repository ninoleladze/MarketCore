import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="nf-page">
      <div class="nf-glow"></div>
      <div class="nf-inner">
        <div class="nf-code">404</div>
        <h1 class="nf-title">Page not found</h1>
        <p class="nf-sub">The page you're looking for doesn't exist or has been moved.</p>
        <div class="nf-actions">
          <a routerLink="/" class="btn-hero-primary">Go Home</a>
          <a routerLink="/products" class="btn-hero-ghost">Browse Products</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .nf-page {
      min-height: 100vh;
      background: var(--navy-950);
      display: flex;
      align-items: center;
      justify-content: center;
      text-align: center;
      padding: 2rem;
      position: relative;
      overflow: hidden;
    }
    .nf-glow {
      position: absolute;
      width: 500px; height: 500px;
      background: radial-gradient(circle, rgba(196,92,0,0.12) 0%, transparent 70%);
      top: 50%; left: 50%;
      transform: translate(-50%, -50%);
      pointer-events: none;
    }
    .nf-inner { position: relative; z-index: 1; }
    .nf-code {
      font-family: var(--font-display);
      font-size: clamp(7rem, 20vw, 14rem);
      font-weight: 400;
      line-height: 1;
      color: transparent;
      -webkit-text-stroke: 1px rgba(232,119,34,0.25);
      letter-spacing: -0.04em;
      margin-bottom: 1.5rem;
      user-select: none;
    }
    .nf-title {
      font-family: var(--font-display);
      font-size: clamp(1.5rem, 3vw, 2.2rem);
      font-weight: 400;
      color: var(--neutral-100);
      margin: 0 0 1rem;
    }
    .nf-sub {
      color: var(--neutral-500);
      font-size: 1rem;
      margin: 0 0 2.5rem;
      max-width: 380px;
      margin-left: auto;
      margin-right: auto;
    }
    .nf-actions {
      display: flex;
      gap: 1rem;
      justify-content: center;
      flex-wrap: wrap;
    }
    .btn-hero-primary {
      padding: 0.9rem 2rem;
      background: linear-gradient(135deg, var(--orange-600), var(--orange-400));
      color: #fff;
      border-radius: 999px;
      font-weight: 600;
      font-size: 0.88rem;
      letter-spacing: 0.04em;
      text-decoration: none;
      transition: transform 0.3s, box-shadow 0.3s;
      box-shadow: 0 4px 20px var(--red-glow-soft);
    }
    .btn-hero-primary:hover {
      transform: translateY(-3px);
      box-shadow: 0 8px 32px var(--red-glow);
    }
    .btn-hero-ghost {
      padding: 0.9rem 2rem;
      background: rgba(255,255,255,0.05);
      color: var(--neutral-200);
      border-radius: 999px;
      border: 1px solid rgba(255,255,255,0.12);
      font-size: 0.88rem;
      text-decoration: none;
      transition: background 0.2s, border-color 0.2s;
    }
    .btn-hero-ghost:hover {
      background: rgba(232,119,34,0.1);
      border-color: rgba(232,119,34,0.3);
      color: #fff;
    }
  `]
})
export class NotFoundComponent {}
