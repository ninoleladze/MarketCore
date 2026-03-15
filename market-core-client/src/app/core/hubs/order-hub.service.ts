import { Injectable, inject, OnDestroy } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../services/auth.service';

export interface OrderStatusChangedEvent {
  orderId: string;
  newStatus: string;
  updatedAt: string;
}

export interface NewOrderPlacedEvent {
  orderId: string;
  totalAmount: number;
}

@Injectable({ providedIn: 'root' })
export class OrderHubService implements OnDestroy {
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;

  private readonly statusChangedSubject = new Subject<OrderStatusChangedEvent>();
  private readonly newOrderSubject = new Subject<NewOrderPlacedEvent>();

  readonly orderStatusChanged$: Observable<OrderStatusChangedEvent> =
    this.statusChangedSubject.asObservable();

  readonly newOrderPlaced$: Observable<NewOrderPlacedEvent> =
    this.newOrderSubject.asObservable();

  get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  async startConnection(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${this.hubBaseUrl}/hubs/orders`, {
        accessTokenFactory: () => this.auth.getToken() ?? ''
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('OrderStatusChanged', (payload: OrderStatusChangedEvent) => {
      this.statusChangedSubject.next(payload);
    });

    this.connection.on('NewOrderPlaced', (payload: NewOrderPlacedEvent) => {
      this.newOrderSubject.next(payload);
    });

    try {
      await this.connection.start();
    } catch (err) {
      console.error('[OrderHubService] Failed to start SignalR connection:', err);
    }
  }

  async stopConnection(): Promise<void> {
    if (
      this.connection &&
      this.connection.state !== HubConnectionState.Disconnected
    ) {
      try {
        await this.connection.stop();
      } catch (err) {
        console.error('[OrderHubService] Error stopping SignalR connection:', err);
      }
    }
    this.connection = null;
  }

  async joinOrderGroup(orderId: string): Promise<void> {
    if (!this.isConnected || !orderId) return;
    try {
      await this.connection!.invoke('JoinOrderGroup', orderId);
    } catch (err) {
      console.error('[OrderHubService] JoinOrderGroup failed:', err);
    }
  }

  async leaveOrderGroup(orderId: string): Promise<void> {
    if (!this.isConnected || !orderId) return;
    try {
      await this.connection!.invoke('LeaveOrderGroup', orderId);
    } catch (err) {
      console.error('[OrderHubService] LeaveOrderGroup failed:', err);
    }
  }

  ngOnDestroy(): void {
    this.statusChangedSubject.complete();
    this.newOrderSubject.complete();
    this.stopConnection();
  }

  private get hubBaseUrl(): string {
    return environment.apiUrl.replace(/\/api\/v\d+$/, '');
  }
}
