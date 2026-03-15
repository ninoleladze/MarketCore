import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Cart, AddToCartCommand } from '../models/cart.models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly apiUrl = `${environment.apiUrl}/Cart`;

  readonly itemCount = signal<number>(0);

  constructor(private http: HttpClient) {}

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.apiUrl).pipe(
      tap(cart => setTimeout(() => this.itemCount.set(cart.items?.length ?? 0), 0))
    );
  }

  addItem(command: AddToCartCommand): Observable<Cart> {
    return this.http.post<Cart>(`${this.apiUrl}/items`, command).pipe(
      tap(cart => setTimeout(() => this.itemCount.set(cart.items?.length ?? 0), 0))
    );
  }

  removeItem(productId: string): Observable<Cart> {
    return this.http.delete<Cart>(`${this.apiUrl}/items/${productId}`).pipe(
      tap(cart => setTimeout(() => this.itemCount.set(cart.items?.length ?? 0), 0))
    );
  }

  clearCart(): Observable<void> {
    return this.http.delete<void>(this.apiUrl).pipe(
      tap(() => setTimeout(() => this.itemCount.set(0), 0))
    );
  }
}
