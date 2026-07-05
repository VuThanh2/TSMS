import type { Role } from '@/shared/lib/auth-context';

export interface UserListItem {
  userId: string;
  fullName: string;
  email: string;
  role: Role;
  isActive: boolean;
}

export interface UserDetail extends UserListItem {
  createdAt: string;
  profile?: {
    department?: string;
    major?: string;
  };
}
