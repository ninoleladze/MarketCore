import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectionStrategy,
  ChangeDetectorRef
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderHubService } from '../../../core/hubs/order-hub.service';
import { Order } from '../../../core/models/order.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderListComponent implements OnInit, OnDestroy {
  private readonly orderService = inject(OrderService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly hub = inject(OrderHubService);

  orders: Order[] = [];
  loading = true;

  private statusSub?: Subscription;
  private newOrderSub?: Subscription;

  ngOnInit(): void {
    this.orderService.getOrders().subscribe({
      next: orders => {
        this.orders = orders ?? [];
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Failed to load orders.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });

    this.connectHub();
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
    this.newOrderSub?.unsubscribe();
    this.hub.stopConnection();
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending:   'badge-pending',
      Confirmed: 'badge-confirmed',
      Shipped:   'badge-shipped',
      Delivered: 'badge-delivered',
      Cancelled: 'badge-cancelled'
    };
    return map[status] ?? 'badge-pending';
  }

  private connectHub(): void {
    this.hub.startConnection();

    this.statusSub = this.hub.orderStatusChanged$.subscribe(event => {
      const idx = this.orders.findIndex(o => o.id === event.orderId);
      if (idx !== -1) {
        this.orders = this.orders.map((o, i) =>
          i === idx ? { ...o, status: event.newStatus } : o
        );
        this.cdr.markForCheck();
      }
    });

    this.newOrderSub = this.hub.newOrderPlaced$.subscribe(event => {
      this.toast.info(`New order placed — Total: $${event.totalAmount.toFixed(2)}`);

      this.orderService.getOrders().subscribe({
        next: orders => {
          this.orders = orders ?? [];
          this.cdr.markForCheck();
        }
      });
    });
  }
}
