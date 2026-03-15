import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Product } from '../../../core/models/product.models';
import { Category } from '../../../core/models/category.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly cartService = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly auth = inject(AuthService);

  products: Product[] = [];
  categories: Category[] = [];
  loading = true;

  searchTerm = '';
  selectedCategoryId = '';
  page = 1;
  pageSize = 20;
  totalCount = 0;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.selectedCategoryId = params['categoryId'] ?? '';
      this.searchTerm = params['search'] ?? '';
      this.page = Number(params['page']) || 1;
      this.load();
    });

    this.categoryService.getCategories().subscribe({
      next: cats => { this.categories = cats ?? []; this.cdr.markForCheck(); },
      error: () => {}
    });
  }

  load(): void {
    this.loading = true;
    this.productService.getProducts(this.searchTerm, this.selectedCategoryId, this.page, this.pageSize).subscribe({
      next: result => {
        this.products = result.items ?? [];
        this.totalCount = result.totalCount ?? 0;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Failed to load products.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  search(): void {
    this.page = 1;
    this.updateQueryParams();
  }

  filterByCategory(): void {
    this.page = 1;
    this.updateQueryParams();
  }

  goToPage(p: number): void {
    this.page = p;
    this.updateQueryParams();
  }

  private updateQueryParams(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        search: this.searchTerm || null,
        categoryId: this.selectedCategoryId || null,
        page: this.page > 1 ? this.page : null
      },
      queryParamsHandling: 'merge'
    });
  }

  addToCart(product: Product): void {
    if (!this.auth.isLoggedIn()) {
      this.toast.warning('Please login to add items to your cart.');
      return;
    }
    this.cartService.addItem({ productId: product.id, quantity: 1 }).subscribe({
      next: () => this.toast.success(`${product.name} added to cart!`),
      error: () => this.toast.error('Could not add item to cart.')
    });
  }
}
