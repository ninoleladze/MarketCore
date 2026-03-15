import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Review, AddReviewRequest } from '../models/review.models';

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getReviews(productId: string): Observable<Review[]> {
    return this.http.get<Review[]>(`${this.apiUrl}/products/${productId}/reviews`);
  }

  addReview(productId: string, request: AddReviewRequest): Observable<Review> {
    return this.http.post<Review>(`${this.apiUrl}/products/${productId}/reviews`, request);
  }
}
