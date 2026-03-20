import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { DecimalPipe, DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { CartService } from '../../../core/services/cart.service';
import { ReviewService } from '../../../core/services/review.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { Product } from '../../../core/models/product.models';
import { Review } from '../../../core/models/review.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, FormsModule, DecimalPipe, DatePipe, LoadingSpinnerComponent],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);
  private readonly reviewService = inject(ReviewService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  product: Product | null = null;
  reviews: Review[] = [];
  loading = true;
  quantity = 1;
  addingToCart = false;
  submittingReview = false;
  selectedImg = 0;
  private touchStartX = 0;

  reviewForm = this.fb.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['', [Validators.required, Validators.minLength(10)]]
  });

  get stars(): number[] { return [1, 2, 3, 4, 5]; }

  get galleryImages(): string[] {
    if (this.product?.images?.length) return this.product.images;
    if (this.product?.imageUrl) return [this.product.imageUrl];
    return [];
  }

  selectImg(i: number): void {
    this.selectedImg = i;
    this.cdr.markForCheck();
  }

  prevImg(): void {
    const len = this.galleryImages.length;
    this.selectedImg = (this.selectedImg - 1 + len) % len;
    this.cdr.markForCheck();
  }

  nextImg(): void {
    const len = this.galleryImages.length;
    this.selectedImg = (this.selectedImg + 1) % len;
    this.cdr.markForCheck();
  }

  onTouchStart(e: TouchEvent): void {
    this.touchStartX = e.touches[0].clientX;
  }

  onTouchEnd(e: TouchEvent): void {
    const delta = e.changedTouches[0].clientX - this.touchStartX;
    if (Math.abs(delta) > 40) {
      delta < 0 ? this.nextImg() : this.prevImg();
    }
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.productService.getProduct(id).subscribe({
      next: p => {
        this.product = p;
        this.loading = false;
        this.cdr.markForCheck();
        this.loadReviews(id);
      },
      error: () => {
        this.toast.error('Product not found.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadReviews(productId: string): void {
    this.reviewService.getReviews(productId).subscribe({
      next: reviews => { this.reviews = reviews ?? []; this.cdr.markForCheck(); },
      error: () => {}
    });
  }

  addToCart(): void {
    if (!this.product) return;
    if (!this.auth.isLoggedIn()) {
      this.toast.warning('Please login to add items to your cart.');
      return;
    }
    this.addingToCart = true;
    this.cartService.addItem({ productId: this.product.id, quantity: this.quantity }).subscribe({
      next: () => {
        this.toast.success(`${this.product!.name} added to cart!`);
        this.addingToCart = false;
      },
      error: () => {
        this.toast.error('Could not add item to cart.');
        this.addingToCart = false;
      }
    });
  }

  submitReview(): void {
    if (!this.product || this.reviewForm.invalid) {
      this.reviewForm.markAllAsTouched();
      return;
    }
    this.submittingReview = true;
    const { rating, comment } = this.reviewForm.value;
    this.reviewService.addReview(this.product.id, { rating: rating!, comment: comment! }).subscribe({
      next: review => {
        this.reviews = [review, ...this.reviews];
        this.reviewForm.reset({ rating: 5, comment: '' });
        this.toast.success('Review submitted!');
        this.submittingReview = false;
      },
      error: (err) => {
        const msg = err.error?.message ?? err.error?.title ?? 'Could not submit review.';
        this.toast.error(msg);
        this.submittingReview = false;
        this.cdr.markForCheck();
      }
    });
  }

  averageRating(): number {
    if (!this.reviews.length) return 0;
    const sum = this.reviews.reduce((acc, r) => acc + r.rating, 0);
    return Math.round((sum / this.reviews.length) * 10) / 10;
  }

  starFilled(star: number): boolean {
    return star <= this.averageRating();
  }
}
