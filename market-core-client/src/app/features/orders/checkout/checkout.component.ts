import { Component, OnInit, inject, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { CartService } from '../../../core/services/cart.service';
import { ToastService } from '../../../core/services/toast.service';
import { Cart } from '../../../core/models/cart.models';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule, DecimalPipe, LoadingSpinnerComponent],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CheckoutComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly cartService = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  cart: Cart | null = null;
  loading = true;
  placing = false;

  form = this.fb.group({
    shippingStreet: ['', Validators.required],
    shippingCity: ['', Validators.required],
    shippingState: ['', Validators.required],
    shippingZipCode: ['', Validators.required],
    shippingCountry: ['', Validators.required]
  });

  get f() { return this.form.controls; }

  ngOnInit(): void {
    this.cartService.getCart().subscribe({
      next: cart => {
        this.cart = cart;
        this.loading = false;
        this.cdr.markForCheck();
        if (!cart?.items?.length) {
          this.toast.warning('Your cart is empty.');
          this.router.navigate(['/cart']);
        }
      },
      error: () => {
        this.loading = false;
        this.cdr.markForCheck();
        this.router.navigate(['/cart']);
      }
    });
  }

  placeOrder(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.placing = true;
    this.orderService.checkout(this.form.value as any).subscribe({
      next: orderId => {
        this.toast.success('Order placed successfully!');
        this.router.navigate(['/orders', orderId]);
      },
      error: (err) => {
        const msg = err.error?.message ?? err.error?.title ?? 'Failed to place order.';
        this.toast.error(msg);
        this.placing = false;
        this.cdr.markForCheck();
      }
    });
  }
}
