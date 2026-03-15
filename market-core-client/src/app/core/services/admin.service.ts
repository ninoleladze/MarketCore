import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from '../models/product.models';

export interface AdminStats {
  totalProducts: number;
  totalUsers: number;
  totalOrders: number;
  totalRevenue: number;
}

export interface AdminProduct {
  id: string;
  name: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  imageUrl?: string;
  isActive: boolean;
}

export interface AdminOrderSummary {
  id: string;
  userId: string;
  status: string;
  totalAmount: number;
  currency: string;
  itemCount: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly adminUrl    = `${environment.apiUrl}/Admin`;
  private readonly productsUrl = `${environment.apiUrl}/Products`;

  constructor(private readonly http: HttpClient) {}

  getStats(): Observable<AdminStats> {
    return this.http.get<AdminStats>(`${this.adminUrl}/stats`);
  }

  getProducts(page = 1, pageSize = 50): Observable<PagedResult<AdminProduct>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<AdminProduct>>(`${this.adminUrl}/products`, { params });
  }

  getAdminOrders(page = 1, pageSize = 20): Observable<PagedResult<AdminOrderSummary>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    return this.http.get<PagedResult<AdminOrderSummary>>(`${this.adminUrl}/orders`, { params });
  }

  updateOrderStatus(id: string, newStatus: string): Observable<void> {
    return this.http.patch<void>(`${this.adminUrl}/orders/${id}/status`, { newStatus });
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.productsUrl}/${id}`);
  }
}
