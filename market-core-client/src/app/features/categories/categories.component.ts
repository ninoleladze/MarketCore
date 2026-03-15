import { Component, OnInit, inject } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { CategoryService } from '../../core/services/category.service';
import { ToastService } from '../../core/services/toast.service';
import { Category } from '../../core/models/category.models';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.css'
})
export class CategoriesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly categoryService = inject(CategoryService);
  private readonly toast = inject(ToastService);

  categories: Category[] = [];
  loading = true;
  saving = false;
  editingId: string | null = null;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', Validators.required]
  });

  get f() { return this.form.controls; }

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getCategories().subscribe({
      next: cats => {
        this.categories = cats ?? [];
        this.loading = false;
      },
      error: () => {
        this.toast.error('Failed to load categories.');
        this.loading = false;
      }
    });
  }

  startEdit(cat: Category): void {
    this.editingId = cat.id;
    this.form.setValue({ name: cat.name, description: cat.description });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving = true;
    const command = this.form.value as { name: string; description: string };

    if (this.editingId) {
      this.categoryService.updateCategory(this.editingId, command).subscribe({
        next: updated => {
          const idx = this.categories.findIndex(c => c.id === this.editingId);
          if (idx > -1) this.categories[idx] = updated;
          this.toast.success('Category updated.');
          this.cancelEdit();
          this.saving = false;
        },
        error: () => {
          this.toast.error('Failed to update category.');
          this.saving = false;
        }
      });
    } else {
      this.categoryService.createCategory(command).subscribe({
        next: cat => {
          this.categories.push(cat);
          this.form.reset();
          this.toast.success('Category created.');
          this.saving = false;
        },
        error: () => {
          this.toast.error('Failed to create category.');
          this.saving = false;
        }
      });
    }
  }

  deleteCategory(cat: Category): void {
    if (!confirm(`Delete category "${cat.name}"?`)) return;
    this.categoryService.deleteCategory(cat.id).subscribe({
      next: () => {
        this.categories = this.categories.filter(c => c.id !== cat.id);
        this.toast.success('Category deleted.');
      },
      error: () => this.toast.error('Failed to delete category.')
    });
  }
}
