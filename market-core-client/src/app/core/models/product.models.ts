export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  imageUrl?: string;
  sellerId: string;
  isActive: boolean;
}

export interface CreateProductCommand {
  name: string;
  description: string;
  price: number;
  currency: string;
  stockQuantity: number;
  categoryId: string;
  imageUrl?: string;
}

export interface UpdateProductRequest {
  name: string;
  description: string;
  price: number;
  currency: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
