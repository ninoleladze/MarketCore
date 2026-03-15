import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Category, CreateCategoryCommand } from '../models/category.models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly apiUrl = `${environment.apiUrl}/Categories`;

  constructor(private http: HttpClient) {}

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(this.apiUrl);
  }

  createCategory(command: CreateCategoryCommand): Observable<Category> {
    return this.http.post<Category>(this.apiUrl, command);
  }

  updateCategory(id: string, command: CreateCategoryCommand): Observable<Category> {
    return this.http.put<Category>(`${this.apiUrl}/${id}`, command);
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
