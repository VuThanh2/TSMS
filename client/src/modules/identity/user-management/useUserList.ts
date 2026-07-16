import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { useTableSort } from '@/shared/hooks/useTableSort';
import {
  getUsersApi,
  getUserByIdApi,
  createUserApi,
  updateUserApi,
  updateUserStatusApi,
  importUsersCsvApi,
} from './user-management.api';

export function useUserList() {
  const [search, setSearch] = useState('');
  const [role, setRole] = useState<string>('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const { sort, applySorter } = useTableSort();

  // Debounce: chỉ gọi API sau khi ngừng gõ, tránh 1 request mỗi keystroke gây lag.
  const debouncedSearch = useDebouncedValue(search);

  const { data, isLoading } = useQuery({
    // sort nằm trong queryKey → đổi sort là refetch, không phải sort lại 10 dòng có sẵn.
    queryKey: ['users', { search: debouncedSearch, role, page, pageSize, ...sort }],
    queryFn: () =>
      getUsersApi({
        search: debouncedSearch || undefined,
        role: role || undefined,
        page,
        pageSize,
        sortBy: sort.sortBy,
        sortDir: sort.sortDir,
      }),
    select: (res) => res.data,
  });

  return {
    users: data?.items ?? [],
    totalCount: data?.totalCount ?? 0,
    isLoading,
    search, setSearch,
    role, setRole,
    page, setPage,
    pageSize, setPageSize,
    sort, applySorter,
  };
}

export function useUserDetail(userId: string | null) {
  return useQuery({
    queryKey: ['user', userId],
    queryFn: () => getUserByIdApi(userId!),
    select: (res) => res.data,
    enabled: !!userId,
  });
}

const STATUS_ERROR_MESSAGES: Record<string, string> = {
  'User.CannotDeactivateSelf': 'You can’t deactivate your own account',
  'User.LecturerHasActiveCourses': 'Lecturer still has active courses',
  'User.StudentHasActiveEnrollments': 'Student still has active enrollments',
};

export function useCreateUser(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createUserApi,
    onSuccess: () => {
      void message.success('User created');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      // Key phải là mã ĐẦY ĐỦ kèm prefix 'User.' — khớp Error.Create bên Identity BC.
      const messages: Record<string, string> = {
        'User.EmailAlreadyInUse': 'Email already in use',
        'User.InvalidRole': 'Invalid role',
        'User.PasswordPolicyViolation': 'Password needs 6+ characters with upper, lower and a number',
      };
      void message.error(messages[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useEditUser(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, ...data }: { userId: string; fullName: string; email: string; department?: string; major?: string }) =>
      updateUserApi(userId, data),
    onSuccess: () => {
      void message.success('User updated');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useToggleUserStatus() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      updateUserStatusApi(userId, { isActive }),
    onSuccess: () => {
      void message.success('Status updated');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      void message.error(STATUS_ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Something went wrong');
    },
  });
}

export function useImportCsv() {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: importUsersCsvApi,
    onSuccess: (res) => {
      const { successCount, failureCount } = res.data;
      if (failureCount === 0) {
        void message.success(`${successCount} users imported`);
      } else {
        void message.warning(`${successCount} imported · ${failureCount} failed`);
      }
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Could not import CSV');
    },
  });
}
