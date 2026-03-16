import {
  Component,
  ChangeDetectionStrategy,
  OnInit,
  OnDestroy,
  signal,
  inject
} from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { AdminService, AdminStats, AdminProduct } from '../../core/services/admin.service';
import { OrderHubService } from '../../core/hubs/order-hub.service';
import { ToastService } from '../../core/services/toast.service';

interface AdminOrderSummary {
  id: string;
  userId: string;
  status: string;
  totalAmount: number;
  currency: string;
  itemCount: number;
  createdAt: string;
}

const ORDER_STATUSES = ['Confirmed', 'Shipped', 'Delivered', 'Cancelled'] as const;
type OrderStatus = (typeof ORDER_STATUSES)[number];

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, CurrencyPipe, RouterLink],
  template: `
    <div class="admin-page">
      <div class="admin-container">

        <!-- Page header -->
        <header class="admin-header">
          <p class="section-eyebrow">Administration</p>
          <h1 class="admin-title">Admin Dashboard</h1>
          <p class="admin-subtitle">Platform overview, product and order management</p>
        </header>

        <!-- Stats loading skeleton -->
        @if (statsLoading()) {
          <div class="stats-grid">
            @for (i of [0,1,2,3]; track i) {
              <div class="stat-card stat-card--skeleton">
                <div class="skeleton-icon"></div>
                <div class="skeleton-number"></div>
                <div class="skeleton-label"></div>
              </div>
            }
          </div>
        }

        <!-- Stats error -->
        @if (statsError()) {
          <div class="error-banner">
            <span class="error-icon">!</span>
            <span>{{ statsError() }}</span>
          </div>
        }

        <!-- Stats cards -->
        @if (stats() && !statsLoading()) {
          <div class="stats-grid">
            <div class="stat-card">
              <div class="stat-icon stat-icon--products">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
                </svg>
              </div>
              <span class="stat-number">{{ stats()!.totalProducts | number }}</span>
              <span class="stat-label">Total Products</span>
            </div>

            <div class="stat-card">
              <div class="stat-icon stat-icon--users">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                  <circle cx="9" cy="7" r="4"/>
                  <path d="M23 21v-2a4 4 0 0 0-3-3.87"/>
                  <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
                </svg>
              </div>
              <span class="stat-number">{{ stats()!.totalUsers | number }}</span>
              <span class="stat-label">Registered Users</span>
            </div>

            <div class="stat-card">
              <div class="stat-icon stat-icon--orders">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2"/>
                  <rect x="9" y="3" width="6" height="4" rx="2"/>
                  <path d="M9 12h6M9 16h4"/>
                </svg>
              </div>
              <span class="stat-number">{{ stats()!.totalOrders | number }}</span>
              <span class="stat-label">Total Orders</span>
            </div>

            <div class="stat-card stat-card--revenue">
              <div class="stat-icon stat-icon--revenue">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="12" y1="1" x2="12" y2="23"/>
                  <path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/>
                </svg>
              </div>
              <span class="stat-number">{{ stats()!.totalRevenue | currency:'USD':'symbol':'1.0-0' }}</span>
              <span class="stat-label">Gross Revenue</span>
            </div>
          </div>
        }

        <!-- Tab navigation -->
        <div class="tab-bar">
          <button class="tab-btn" [class.tab-btn--active]="activeTab() === 'products'" (click)="activeTab.set('products')">
            Products
          </button>
          <button class="tab-btn" [class.tab-btn--active]="activeTab() === 'orders'" (click)="switchToOrders()">
            Orders
            @if (newOrderBadge() > 0) {
              <span class="badge-count">{{ newOrderBadge() }}</span>
            }
          </button>
        </div>

        <!-- ── Products tab ─────────────────────────────────────────── -->
        @if (activeTab() === 'products') {
          <section class="tab-section">
            <div class="section-header">
              <div>
                <p class="section-eyebrow">Catalogue</p>
                <h2 class="section-title">Product Management</h2>
              </div>
              <a routerLink="/products/create" class="btn-primary btn-sm">+ Add Product</a>
            </div>

            @if (productsLoading()) {
              <div class="table-skeleton">
                @for (i of [0,1,2,3,4]; track i) {
                  <div class="table-skeleton-row"></div>
                }
              </div>
            }

            @if (productsError()) {
              <div class="error-banner">
                <span class="error-icon">!</span>
                <span>{{ productsError() }}</span>
              </div>
            }

            @if (!productsLoading() && !productsError() && products().length > 0) {
              <div class="table-wrapper">
                <table class="products-table">
                  <thead>
                    <tr>
                      <th>Product</th>
                      <th>Category</th>
                      <th class="text-right">Price</th>
                      <th class="text-right">Stock</th>
                      <th class="text-center">Status</th>
                      <th class="text-center">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (product of products(); track product.id) {
                      <tr class="product-row" [class.row--inactive]="!product.isActive">
                        <td class="product-name-cell">
                          @if (product.imageUrl) {
                            <img [src]="product.imageUrl" [alt]="product.name" class="product-thumb" />
                          } @else {
                            <div class="product-thumb product-thumb--placeholder">
                              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5">
                                <rect x="3" y="3" width="18" height="18" rx="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                              </svg>
                            </div>
                          }
                          <span class="product-name">{{ product.name }}</span>
                        </td>
                        <td class="cell-muted">{{ product.categoryName }}</td>
                        <td class="text-right">{{ product.price | currency:product.currency:'symbol':'1.2-2' }}</td>
                        <td class="text-right" [class.stock-low]="product.stockQuantity < 5">
                          {{ product.stockQuantity }}
                        </td>
                        <td class="text-center">
                          @if (product.isActive) {
                            <span class="status-pill status-pill--active">Active</span>
                          } @else {
                            <span class="status-pill status-pill--inactive">Inactive</span>
                          }
                        </td>
                        <td class="text-center">
                          <div class="action-group">
                            <a [routerLink]="['/products', product.id]" class="action-btn action-btn--view" title="View product">
                              <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>
                              </svg>
                            </a>
                            <button
                              class="action-btn action-btn--delete"
                              title="Delete product"
                              [disabled]="deletingId() === product.id"
                              (click)="confirmDelete(product)">
                              @if (deletingId() === product.id) {
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="spin">
                                  <path d="M21 12a9 9 0 1 1-6.219-8.56"/>
                                </svg>
                              } @else {
                                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                  <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/><path d="M10 11v6M14 11v6"/><path d="M9 6V4h6v2"/>
                                </svg>
                              }
                            </button>
                          </div>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }

            @if (!productsLoading() && !productsError() && products().length === 0) {
              <div class="empty-state">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2">
                  <path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/>
                </svg>
                <p>No products found.</p>
                <a routerLink="/products/create" class="btn-primary btn-sm">Create the first product</a>
              </div>
            }
          </section>
        }

        <!-- ── Orders tab ───────────────────────────────────────────── -->
        @if (activeTab() === 'orders') {
          <section class="tab-section">
            <div class="section-header">
              <div>
                <p class="section-eyebrow">Fulfilment</p>
                <h2 class="section-title">Order Management</h2>
              </div>
            </div>

            @if (ordersLoading()) {
              <div class="table-skeleton">
                @for (i of [0,1,2,3,4]; track i) {
                  <div class="table-skeleton-row"></div>
                }
              </div>
            }

            @if (ordersError()) {
              <div class="error-banner">
                <span class="error-icon">!</span>
                <span>{{ ordersError() }}</span>
              </div>
            }

            @if (!ordersLoading() && !ordersError() && orders().length > 0) {
              <div class="table-wrapper">
                <table class="products-table orders-table">
                  <thead>
                    <tr>
                      <th>Order ID</th>
                      <th>User</th>
                      <th class="text-right">Total</th>
                      <th class="text-center">Items</th>
                      <th class="text-center">Status</th>
                      <th>Date</th>
                      <th class="text-center">Change Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    @for (order of orders(); track order.id) {
                      <tr class="product-row">
                        <td class="order-id-cell">
                          #{{ order.id.slice(0, 8).toUpperCase() }}
                        </td>
                        <td class="cell-muted mono">{{ order.userId.slice(0, 8) }}...</td>
                        <td class="text-right">{{ order.totalAmount | currency:order.currency:'symbol':'1.2-2' }}</td>
                        <td class="text-center cell-muted">{{ order.itemCount }}</td>
                        <td class="text-center">
                          <span class="order-status-pill order-status-pill--{{ order.status.toLowerCase() }}">
                            {{ order.status }}
                          </span>
                        </td>
                        <td class="cell-muted text-sm">{{ order.createdAt | date:'mediumDate' }}</td>
                        <td class="text-center">
                          <select
                            class="status-select"
                            [disabled]="updatingOrderId() === order.id || order.status === 'Delivered' || order.status === 'Cancelled'"
                            (change)="onStatusChange(order, $event)">
                            <option value="">-- Update --</option>
                            @for (status of availableStatuses(order.status); track status) {
                              <option [value]="status">{{ status }}</option>
                            }
                          </select>
                        </td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }

            @if (!ordersLoading() && !ordersError() && orders().length === 0) {
              <div class="empty-state">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.2">
                  <path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2"/>
                  <rect x="9" y="3" width="6" height="4" rx="2"/>
                </svg>
                <p>No orders yet.</p>
              </div>
            }
          </section>
        }

      </div>
    </div>

    <!-- Delete confirmation modal -->
    @if (pendingDelete()) {
      <div class="modal-backdrop" (click)="cancelDelete()">
        <div class="modal" (click)="$event.stopPropagation()">
          <h3 class="modal-title">Delete Product</h3>
          <p class="modal-body">
            Are you sure you want to deactivate
            <strong>{{ pendingDelete()!.name }}</strong>?
            This action cannot be undone.
          </p>
          <div class="modal-actions">
            <button class="btn-ghost btn-sm" (click)="cancelDelete()">Cancel</button>
            <button class="btn-danger btn-sm" (click)="executeDelete()">Delete</button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .admin-page {
      min-height: 100vh;
      background: var(--navy-950);
      padding-top: 7rem;
      padding-bottom: 5rem;
    }

    .admin-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 0 2rem;
    }

    /* ── Header ─────────────────────────────────────── */
    .admin-header {
      margin-bottom: 2.5rem;
    }

    .admin-title {
      font-family: var(--font-display);
      font-size: clamp(2rem, 4vw, 2.8rem);
      font-weight: 700;
      color: var(--neutral-50);
      line-height: 1.15;
      margin-bottom: 0.4rem;
    }

    .admin-subtitle {
      color: var(--neutral-400);
      font-size: 0.95rem;
      margin: 0;
    }

    /* ── Stat cards ─────────────────────────────────── */
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1.25rem;
      margin-bottom: 2.5rem;
    }

    .stat-card {
      background: var(--navy-800);
      border: 1px solid var(--color-border);
      border-radius: 14px;
      padding: 1.6rem 1.5rem 1.4rem;
      display: flex;
      flex-direction: column;
      align-items: flex-start;
      gap: 0.5rem;
      position: relative;
      overflow: hidden;
      transition: border-color var(--transition), box-shadow var(--transition),
                  transform var(--transition);
    }

    .stat-card::before {
      content: '';
      position: absolute;
      inset: 0;
      background: linear-gradient(135deg, rgba(232,119,34,0.04) 0%, transparent 60%);
      pointer-events: none;
    }

    .stat-card:hover {
      border-color: rgba(232, 119, 34, 0.35);
      box-shadow: 0 0 0 1px rgba(232,119,34,0.1), 0 8px 32px rgba(0,0,0,0.4), 0 0 20px rgba(232,119,34,0.06);
      transform: translateY(-2px);
    }

    .stat-card--revenue {
      border-color: rgba(232,119,34,0.2);
    }
    .stat-card--revenue:hover {
      border-color: rgba(232,119,34,0.5);
      box-shadow: 0 0 0 1px rgba(232,119,34,0.15), 0 8px 32px rgba(0,0,0,0.4), 0 0 28px rgba(232,119,34,0.1);
    }

    .stat-icon {
      width: 42px;
      height: 42px;
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 0.4rem;
    }

    .stat-icon svg {
      width: 20px;
      height: 20px;
    }

    .stat-icon--products { background: rgba(21, 101, 192, 0.15); color: #64b5f6; }
    .stat-icon--users    { background: rgba(106, 27, 154, 0.15); color: #ce93d8; }
    .stat-icon--orders   { background: rgba(230, 81, 0, 0.15);   color: #ffb74d; }
    .stat-icon--revenue  { background: rgba(232, 119, 34, 0.15);   color: var(--orange-400); }

    .stat-number {
      font-size: clamp(1.6rem, 3vw, 2.1rem);
      font-weight: 800;
      color: var(--neutral-50);
      letter-spacing: -0.03em;
      line-height: 1;
      font-family: var(--font-body);
    }

    .stat-label {
      font-size: 0.78rem;
      color: var(--neutral-400);
      font-weight: 500;
      letter-spacing: 0.03em;
      text-transform: uppercase;
    }

    .stat-card--skeleton {
      pointer-events: none;
      animation: pulse 1.6s ease-in-out infinite;
    }

    .skeleton-icon  { width: 42px;  height: 42px; border-radius: 10px; background: var(--navy-700); }
    .skeleton-number{ width: 80px;  height: 28px; border-radius: 6px;  background: var(--navy-700); }
    .skeleton-label { width: 60px;  height: 12px; border-radius: 4px;  background: var(--navy-700); }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.45; }
    }

    /* ── Tab bar ─────────────────────────────────────── */
    .tab-bar {
      display: flex;
      gap: 0.25rem;
      border-bottom: 1px solid var(--color-border);
      margin-bottom: 0;
    }

    .tab-btn {
      position: relative;
      padding: 0.7rem 1.4rem;
      background: transparent;
      border: none;
      border-bottom: 2px solid transparent;
      color: var(--neutral-500);
      font-size: 0.88rem;
      font-weight: 600;
      cursor: pointer;
      transition: color var(--transition), border-color var(--transition);
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
    }

    .tab-btn:hover {
      color: var(--neutral-200);
    }

    .tab-btn--active {
      color: var(--neutral-50);
      border-bottom-color: var(--orange-500);
    }

    .badge-count {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      min-width: 18px;
      height: 18px;
      padding: 0 4px;
      border-radius: 999px;
      background: var(--orange-600);
      color: var(--neutral-50);
      font-size: 0.68rem;
      font-weight: 800;
    }

    /* ── Tab sections ────────────────────────────────── */
    .tab-section {
      background: var(--navy-800);
      border: 1px solid var(--color-border);
      border-top: none;
      border-radius: 0 0 14px 14px;
      overflow: hidden;
    }

    .section-header {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      padding: 1.6rem 1.75rem 1.25rem;
      border-bottom: 1px solid var(--color-border);
      gap: 1rem;
    }

    .section-title {
      font-family: var(--font-display);
      font-size: 1.35rem;
      font-weight: 700;
      color: var(--neutral-50);
      margin: 0;
    }

    /* ── Table ──────────────────────────────────────── */
    .table-wrapper {
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
    }

    .products-table {
      width: 100%;
      border-collapse: collapse;
      font-size: 0.88rem;
    }

    .products-table thead tr {
      background: var(--navy-700);
    }

    .products-table th {
      padding: 0.85rem 1.25rem;
      text-align: left;
      font-size: 0.72rem;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: var(--neutral-400);
      white-space: nowrap;
      border-bottom: 1px solid var(--color-border);
    }

    .products-table td {
      padding: 0.9rem 1.25rem;
      border-bottom: 1px solid rgba(255,255,255,0.04);
      color: var(--neutral-200);
      vertical-align: middle;
    }

    .product-row {
      transition: background var(--transition);
    }

    .product-row:hover {
      background: rgba(255,255,255,0.025);
    }

    .product-row:last-child td {
      border-bottom: none;
    }

    .row--inactive td { opacity: 0.5; }

    .product-name-cell {
      display: flex;
      align-items: center;
      gap: 0.85rem;
      min-width: 200px;
    }

    .product-thumb {
      width: 40px;
      height: 40px;
      border-radius: 8px;
      object-fit: cover;
      flex-shrink: 0;
      border: 1px solid var(--color-border);
    }

    .product-thumb--placeholder {
      background: var(--navy-700);
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .product-thumb--placeholder svg {
      width: 18px;
      height: 18px;
      color: var(--neutral-500);
    }

    .product-name {
      font-weight: 600;
      color: var(--neutral-50);
      font-size: 0.9rem;
    }

    .cell-muted { color: var(--neutral-400); }
    .mono       { font-family: monospace; font-size: 0.82rem; }
    .text-sm    { font-size: 0.82rem; }
    .text-right  { text-align: right; }
    .text-center { text-align: center; }

    .stock-low {
      color: var(--orange-300);
      font-weight: 700;
    }

    /* ── Order ID cell ───────────────────────────────── */
    .order-id-cell {
      font-family: monospace;
      font-size: 0.82rem;
      font-weight: 700;
      color: var(--orange-300);
    }

    /* ── Status pill (product) ───────────────────────── */
    .status-pill {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 999px;
      font-size: 0.72rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      text-transform: uppercase;
    }

    .status-pill--active {
      background: rgba(46, 125, 50, 0.18);
      color: #81c784;
      border: 1px solid rgba(46, 125, 50, 0.3);
    }

    .status-pill--inactive {
      background: rgba(183, 28, 28, 0.18);
      color: #ef9a9a;
      border: 1px solid rgba(183, 28, 28, 0.3);
    }

    /* ── Order status pills ──────────────────────────── */
    .order-status-pill {
      display: inline-block;
      padding: 3px 10px;
      border-radius: 999px;
      font-size: 0.72rem;
      font-weight: 700;
      letter-spacing: 0.05em;
      text-transform: uppercase;
      white-space: nowrap;
    }

    .order-status-pill--pending {
      background: rgba(245, 158, 11, 0.15);
      color: #fbbf24;
      border: 1px solid rgba(245, 158, 11, 0.3);
    }

    .order-status-pill--confirmed {
      background: rgba(59, 130, 246, 0.15);
      color: #60a5fa;
      border: 1px solid rgba(59, 130, 246, 0.3);
    }

    .order-status-pill--shipped {
      background: rgba(139, 92, 246, 0.15);
      color: #a78bfa;
      border: 1px solid rgba(139, 92, 246, 0.3);
    }

    .order-status-pill--delivered {
      background: rgba(34, 197, 94, 0.15);
      color: #4ade80;
      border: 1px solid rgba(34, 197, 94, 0.3);
    }

    .order-status-pill--cancelled {
      background: rgba(232, 119, 34, 0.15);
      color: var(--orange-400);
      border: 1px solid rgba(232, 119, 34, 0.3);
    }

    /* ── Status select ───────────────────────────────── */
    .status-select {
      background: var(--navy-700);
      border: 1px solid var(--color-border);
      color: var(--neutral-300);
      font-size: 0.78rem;
      padding: 4px 8px;
      border-radius: 6px;
      cursor: pointer;
      outline: none;
      transition: border-color var(--transition);
      max-width: 130px;
    }

    .status-select:hover:not(:disabled) {
      border-color: rgba(232,119,34,0.4);
    }

    .status-select:focus {
      border-color: var(--orange-500);
    }

    .status-select:disabled {
      opacity: 0.35;
      cursor: not-allowed;
    }

    /* ── Action buttons ──────────────────────────────── */
    .action-group {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.4rem;
    }

    .action-btn {
      width: 32px;
      height: 32px;
      border-radius: 8px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: transparent;
      border: 1px solid var(--color-border);
      color: var(--neutral-400);
      transition: background var(--transition), color var(--transition), border-color var(--transition);
      text-decoration: none;
      cursor: pointer;
    }

    .action-btn svg { width: 15px; height: 15px; flex-shrink: 0; }

    .action-btn--view:hover {
      background: rgba(100, 181, 246, 0.12);
      border-color: rgba(100, 181, 246, 0.35);
      color: #64b5f6;
    }

    .action-btn--delete:hover:not(:disabled) {
      background: rgba(232, 119, 34, 0.15);
      border-color: rgba(232, 119, 34, 0.4);
      color: var(--orange-400);
    }

    .action-btn:disabled {
      opacity: 0.4;
      pointer-events: none;
    }

    .spin { animation: spin 0.8s linear infinite; }

    @keyframes spin { to { transform: rotate(360deg); } }

    /* ── Table skeleton ─────────────────────────────── */
    .table-skeleton { padding: 0.5rem 0; }

    .table-skeleton-row {
      height: 56px;
      margin: 0 1.25rem;
      border-radius: 6px;
      background: var(--navy-700);
      margin-bottom: 4px;
      animation: pulse 1.6s ease-in-out infinite;
    }

    .table-skeleton-row:nth-child(even) { animation-delay: 0.3s; }

    /* ── Error banner ───────────────────────────────── */
    .error-banner {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      background: rgba(183, 28, 28, 0.12);
      border: 1px solid rgba(183, 28, 28, 0.3);
      border-radius: 10px;
      padding: 1rem 1.25rem;
      color: #ef9a9a;
      font-size: 0.88rem;
      margin-bottom: 2rem;
    }

    .error-icon {
      width: 22px;
      height: 22px;
      border-radius: 50%;
      background: rgba(183, 28, 28, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 800;
      flex-shrink: 0;
    }

    /* ── Empty state ────────────────────────────────── */
    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
      padding: 4rem 2rem;
      color: var(--neutral-400);
    }

    .empty-state svg { width: 52px; height: 52px; opacity: 0.35; }
    .empty-state p   { font-size: 0.95rem; margin: 0; }

    /* ── Modal ──────────────────────────────────────── */
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(5, 12, 26, 0.75);
      backdrop-filter: blur(6px);
      -webkit-backdrop-filter: blur(6px);
      z-index: 2000;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1rem;
    }

    .modal {
      background: var(--navy-800);
      border: 1px solid rgba(232, 119, 34, 0.2);
      border-radius: 16px;
      padding: 2rem;
      max-width: 420px;
      width: 100%;
      box-shadow: 0 20px 60px rgba(0,0,0,0.6), 0 0 40px rgba(232,119,34,0.06);
      animation: modalIn 0.2s var(--ease-bounce) both;
    }

    @keyframes modalIn {
      from { transform: scale(0.92); opacity: 0; }
      to   { transform: scale(1);    opacity: 1; }
    }

    .modal-title  { font-family: var(--font-display); font-size: 1.2rem; color: var(--neutral-50); margin-bottom: 0.75rem; }
    .modal-body   { color: var(--neutral-400); font-size: 0.9rem; line-height: 1.6; margin-bottom: 1.5rem; }
    .modal-body strong { color: var(--neutral-200); }
    .modal-actions { display: flex; justify-content: flex-end; gap: 0.75rem; }

    /* ── Responsive ─────────────────────────────────── */
    @media (max-width: 1024px) { .stats-grid { grid-template-columns: repeat(2, 1fr); } }

    @media (max-width: 640px) {
      .admin-page { padding-top: 6rem; }
      .admin-container { padding: 0 1rem; }
      .stats-grid { grid-template-columns: 1fr 1fr; gap: 0.75rem; }
      .stat-card  { padding: 1.2rem 1rem 1rem; }
      .section-header { flex-direction: column; align-items: flex-start; padding: 1.25rem 1rem 1rem; }
      .products-table th, .products-table td { padding: 0.75rem; }
      .modal { padding: 1.5rem; }
    }

    @media (max-width: 400px) { .stats-grid { grid-template-columns: 1fr; } }
  `]
})
export class AdminDashboardComponent implements OnInit, OnDestroy {
  private readonly adminService = inject(AdminService);
  private readonly hub = inject(OrderHubService);
  private readonly toast = inject(ToastService);

  readonly activeTab = signal<'products' | 'orders'>('products');

  readonly stats          = signal<AdminStats | null>(null);
  readonly statsLoading   = signal(true);
  readonly statsError     = signal<string | null>(null);

  readonly products        = signal<AdminProduct[]>([]);
  readonly productsLoading = signal(true);
  readonly productsError   = signal<string | null>(null);

  readonly orders        = signal<AdminOrderSummary[]>([]);
  readonly ordersLoading = signal(false);
  readonly ordersError   = signal<string | null>(null);
  readonly updatingOrderId = signal<string | null>(null);

  readonly newOrderBadge = signal(0);

  readonly deletingId    = signal<string | null>(null);
  readonly pendingDelete = signal<AdminProduct | null>(null);

  private newOrderSub?: Subscription;
  private statusSub?: Subscription;

  availableStatuses(currentStatus: string): OrderStatus[] {
    const transitions: Record<string, OrderStatus[]> = {
      Pending:   ['Confirmed', 'Cancelled'],
      Confirmed: ['Shipped',   'Cancelled'],
      Shipped:   ['Delivered'],
      Delivered: [],
      Cancelled: []
    };
    return transitions[currentStatus] ?? [];
  }

  ngOnInit(): void {
    this.loadStats();
    this.loadProducts();
    this.connectHub();
  }

  ngOnDestroy(): void {
    this.newOrderSub?.unsubscribe();
    this.statusSub?.unsubscribe();
    this.hub.stopConnection();
  }

  switchToOrders(): void {
    this.activeTab.set('orders');
    this.newOrderBadge.set(0);
    if (this.orders().length === 0) {
      this.loadOrders();
    }
  }

  onStatusChange(order: AdminOrderSummary, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newStatus = select.value;
    if (!newStatus) return;

    select.value = '';

    this.updatingOrderId.set(order.id);

    this.adminService.updateOrderStatus(order.id, newStatus).subscribe({
      next: () => {
        this.updatingOrderId.set(null);

        this.orders.update(list =>
          list.map(o => o.id === order.id ? { ...o, status: newStatus } : o)
        );
        this.toast.success(`Order #${order.id.slice(0,8).toUpperCase()} → ${newStatus}`);
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.updatingOrderId.set(null);
        this.toast.error(err?.error?.error ?? err?.message ?? 'Failed to update order status.');
      }
    });
  }

  private loadStats(): void {
    this.statsLoading.set(true);
    this.statsError.set(null);

    this.adminService.getStats().subscribe({
      next: data => {
        this.stats.set(data);
        this.statsLoading.set(false);
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.statsError.set(err?.error?.error ?? err?.message ?? 'Failed to load statistics.');
        this.statsLoading.set(false);
      }
    });
  }

  private loadProducts(): void {
    this.productsLoading.set(true);
    this.productsError.set(null);

    this.adminService.getProducts().subscribe({
      next: result => {
        this.products.set(result.items);
        this.productsLoading.set(false);
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.productsError.set(err?.error?.error ?? err?.message ?? 'Failed to load products.');
        this.productsLoading.set(false);
      }
    });
  }

  private loadOrders(): void {
    this.ordersLoading.set(true);
    this.ordersError.set(null);

    this.adminService.getAdminOrders().subscribe({
      next: result => {
        this.orders.set(result.items as AdminOrderSummary[]);
        this.ordersLoading.set(false);
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.ordersError.set(err?.error?.error ?? err?.message ?? 'Failed to load orders.');
        this.ordersLoading.set(false);
      }
    });
  }

  private connectHub(): void {
    this.hub.startConnection();

    this.statusSub = this.hub.orderStatusChanged$.subscribe(event => {
      if (this.activeTab() === 'orders') {
        this.orders.update(list =>
          list.map(o => o.id === event.orderId ? { ...o, status: event.newStatus } : o)
        );
      }
    });

    this.newOrderSub = this.hub.newOrderPlaced$.subscribe(event => {
      this.toast.info(`New order placed — Total: $${event.totalAmount.toFixed(2)}`);

      if (this.activeTab() !== 'orders') {
        this.newOrderBadge.update(n => n + 1);
      } else {

        this.loadOrders();
      }

      this.loadStats();
    });
  }

  confirmDelete(product: AdminProduct): void {
    this.pendingDelete.set(product);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  executeDelete(): void {
    const product = this.pendingDelete();
    if (!product) return;

    this.pendingDelete.set(null);
    this.deletingId.set(product.id);

    this.adminService.deleteProduct(product.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.loadProducts();
        this.loadStats();
      },
      error: (err: { error?: { error?: string }; message?: string }) => {
        this.deletingId.set(null);
        this.productsError.set(err?.error?.error ?? err?.message ?? 'Failed to delete product.');
      }
    });
  }
}
