export interface LoginCommand {
  email: string;
  password: string;
}

export interface RegisterCommand {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthUserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  isEmailVerified: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: AuthUserDto;
}
