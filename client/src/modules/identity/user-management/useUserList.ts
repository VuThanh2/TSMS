import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { App } from 'antd';
import type { AxiosError } from 'axios';

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

  const { data, isLoading } = useQuery({
    queryKey: ['users', { search, role, page, pageSize }],
    queryFn: () =>
      getUsersApi({
        search: search || undefined,
        role: role || undefined,
        page,
        pageSize,
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
  'User.CannotDeactivateSelf': 'You cannot deactivate your own account.',
  'Lecturer.HasActiveCourses': 'This lecturer still has active courses.',
  'Student.HasActiveEnrollment': 'This student still has active enrollments.',
};

export function useCreateUser(onSuccess?: () => void) {
  const { message } = App.useApp();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createUserApi,
    onSuccess: () => {
      void message.success('User created successfully!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const messages: Record<string, string> = {
        EmailAlreadyExists: 'This email already exists in the system.',
        InvalidRole: 'Invalid role.',
        PasswordPolicyViolation: 'Password is not strong enough.',
      };
      void message.error(messages[code] ?? error.response?.data?.message ?? 'Something went wrong.');
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
      void message.success('Updated successfully!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Something went wrong.');
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
      void message.success('Status updated successfully!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      void message.error(STATUS_ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Something went wrong.');
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
        void message.success(`Imported ${successCount} users successfully!`);
      } else {
        void message.warning(`Succeeded: ${successCount}, Failed: ${failureCount}`);
      }
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Failed to import CSV.');
    },
  });
}
