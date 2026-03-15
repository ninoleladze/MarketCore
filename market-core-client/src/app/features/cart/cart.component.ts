import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { Cart } from '../../core/models/cart.models';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [RouterLink, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CartComponent implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly cdr = inject(ChangeDetectorRef);

  cart: Cart | null = null;
  loading = true;
  clearing = false;

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.loading = true;
    this.cartService.getCart().subscribe({
      next: cart => {
        this.cart = cart;
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Failed to load cart.');
        this.loading = false;
        this.cdr.markForCheck();
      }
    });
  }

  removeItem(productId: string, productName: string): void {
    this.cartService.removeItem(productId).subscribe({
      next: cart => {
        this.cart = cart;
        this.toast.success(`${productName} removed from cart.`);
        this.cdr.markForCheck();
      },
      error: () => this.toast.error('Could not remove item.')
    });
  }

  clearCart(): void {
    if (!confirm('Are you sure you want to clear your cart?')) return;
    this.clearing = true;
    this.cartService.clearCart().subscribe({
      next: () => {
        this.cart = null;
        this.clearing = false;
        this.toast.success('Cart cleared.');
        this.cdr.markForCheck();
      },
      error: () => {
        this.toast.error('Could not clear cart.');
        this.clearing = false;
        this.cdr.markForCheck();
      }
    });
  }
}
