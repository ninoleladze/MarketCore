export interface Category {
  id: string;
  name: string;
  description: string;
  parentCategoryId?: string;
}

export interface CreateCategoryCommand {
  name: string;
  description: string;
}
