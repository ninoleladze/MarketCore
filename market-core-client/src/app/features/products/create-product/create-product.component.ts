import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { ToastService } from '../../../core/services/toast.service';
import { Category } from '../../../core/models/category.models';

@Component({
  selector: 'app-create-product',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './create-product.component.html',
  styleUrl: './create-product.component.css'
})
export class CreateProductComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  categories: Category[] = [];
  loading = false;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3)]],
    description: ['', [Validators.required, Validators.minLength(10)]],
    price: [null as number | null, [Validators.required, Validators.min(0.01)]],
    currency: ['USD', Validators.required],
    stockQuantity: [null as number | null, [Validators.required, Validators.min(0)]],
    categoryId: ['', Validators.required],
    imageUrl: ['', [Validators.pattern('https?://.+')]]
  });

  ngOnInit(): void {
    this.categoryService.getCategories().subscribe({
      next: cats => { this.categories = cats ?? []; },
      error: () => this.toast.error('Failed to load categories.')
    });
  }

  get f() { return this.form.controls; }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.productService.createProduct(this.form.value as any).subscribe({
      next: productId => {
        this.toast.success('Product created successfully!');
        this.router.navigate(['/products', productId]);
      },
      error: (err) => {
        const msg = err.error?.message ?? err.error?.title ?? 'Failed to create product.';
        this.toast.error(msg);
        this.loading = false;
      }
    });
  }
}
