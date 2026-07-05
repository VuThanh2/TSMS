import api from '@/shared/lib/axios';
import type { PagedResult } from '@/shared/types/api.types';
import type { UserListItem, UserDetail } from '@/modules/identity/shared/user.types';

export function getUsersApi(params: { search?: string; role?: string; page: number; pageSize: number }) {
  return api.get<PagedResult<UserListItem>>('/users', { params });
}

export function getUserByIdApi(userId: string) {
  return api.get<UserDetail>(`/users/${userId}`);
}

export function createUserApi(data: { fullName: string; email: string; role: string; password: string }) {
  return api.post<UserListItem>('/users', data);
}

export function updateUserApi(userId: string, data: { fullName: string; email: string; department?: string; major?: string }) {
  return api.put<UserDetail>(`/users/${userId}`, data);
}

export function updateUserStatusApi(userId: string, data: { isActive: boolean }) {
  return api.put(`/users/${userId}/status`, data);
}

export function importUsersCsvApi(file: File) {
  const formData = new FormData();
  formData.append('file', file);
  return api.post<{ successCount: number; failureCount: number; errors: { rowNumber: number; reason: string }[] }>(
    '/users/import-csv',
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
}
