export interface CartItem {
  productId: string;
  productName: string;
  imageUrl?: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Cart {
  id: string;
  items: CartItem[];
  totalAmount: number;
}

export interface AddToCartCommand {
  productId: string;
  quantity: number;
}
