import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Order, CheckoutCommand } from '../models/order.models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly apiUrl = `${environment.apiUrl}/Orders`;

  constructor(private http: HttpClient) {}

  checkout(command: CheckoutCommand): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/checkout`, command);
  }

  getOrders(): Observable<Order[]> {
    return this.http.get<Order[]>(this.apiUrl);
  }

  getOrder(id: string): Observable<Order> {
    return this.http.get<Order>(`${this.apiUrl}/${id}`);
  }

  cancelOrder(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/cancel`, {});
  }

  updateOrderStatus(id: string, newStatus: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/status`, { newStatus });
  }
}
