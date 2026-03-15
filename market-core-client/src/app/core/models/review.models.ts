export interface Review {
  id: string;
  rating: number;
  comment: string;
  reviewerName: string;
  createdAt: string;
}

export interface AddReviewRequest {
  rating: number;
  comment: string;
}
