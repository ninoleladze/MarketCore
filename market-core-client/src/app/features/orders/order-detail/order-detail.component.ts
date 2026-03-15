import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  signal
} from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { OrderService } from '../../../core/services/order.service';
import { ToastService } from '../../../core/services/toast.service';
import { OrderHubService } from '../../../core/hubs/order-hub.service';
import { Order } from '../../../core/models/order.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class OrderDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly hub = inject(OrderHubService);

  order: Order | null = null;
  loading = true;
  cancelling = false;

  readonly hubConnected = signal(false);

  private orderId = '';
  private statusSub?: Subscription;

  ngOnInit(): void {
    this.orderId = this.route.snapshot.paramMap.get('id')!;

    this.orderService.getOrder(this.orderId).subscribe({
      next: order => {
        this.order = order;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Order not found.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });

    this.connectHub();
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
    this.hub.leaveOrderGroup(this.orderId).then(() => this.hub.stopConnection());
  }

  cancelOrder(): void {
    if (!this.order) return;
    if (!confirm('Cancel this order?')) return;
    this.cancelling = true;
    this.orderService.cancelOrder(this.order.id).subscribe({
      next: () => {
        this.order!.status = 'Cancelled';
        this.toast.success('Order cancelled.');
        this.cancelling = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Could not cancel order.');
        this.cancelling = false;
        this.cdr.markForCheck();
      }
    });
  }

  isActiveStep(currentStatus: string, step: string): boolean {
    const order = ['Pending', 'Confirmed', 'Shipped', 'Delivered'];
    const currentIdx = order.indexOf(currentStatus);
    const stepIdx = order.indexOf(step);
    return currentIdx >= stepIdx && currentStatus !== 'Cancelled';
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
    this.hub.startConnection().then(() => {
      this.hubConnected.set(this.hub.isConnected);
      this.cdr.markForCheck();
      return this.hub.joinOrderGroup(this.orderId);
    });

    this.statusSub = this.hub.orderStatusChanged$.subscribe(event => {
      if (event.orderId !== this.orderId) return;
      if (this.order) {
        this.order = { ...this.order, status: event.newStatus };
        this.cdr.markForCheck();
      }
    });
  }
}
