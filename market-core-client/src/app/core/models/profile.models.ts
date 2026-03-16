export interface AddressDto {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
}

export interface ProfileDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  isEmailVerified: boolean;
  address?: AddressDto;
  createdAt: string;
  totalOrders: number;
  totalSpent: number;
}

export interface UpdateProfileCommand {
  firstName: string;
  lastName: string;
  street?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
}

export interface ChangePasswordCommand {
  currentPassword: string;
  newPassword: string;
}
