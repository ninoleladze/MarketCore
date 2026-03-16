import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Product,
  CreateProductCommand,
  UpdateProductRequest,
  PagedResult
} from '../models/product.models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly apiUrl = `${environment.apiUrl}/Products`;

  constructor(private http: HttpClient) {}

  getProducts(
    searchTerm = '',
    categoryId = '',
    page = 1,
    pageSize = 20
  ): Observable<PagedResult<Product>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());
    if (searchTerm) params = params.set('searchTerm', searchTerm);
    if (categoryId) params = params.set('categoryId', categoryId);
    return this.http.get<PagedResult<Product>>(this.apiUrl, { params });
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`${this.apiUrl}/${id}`);
  }

  createProduct(command: CreateProductCommand): Observable<string> {
    return this.http.post<string>(this.apiUrl, command);
  }

  updateProduct(id: string, request: UpdateProductRequest): Observable<Product> {
    return this.http.put<Product>(`${this.apiUrl}/${id}`, request);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
