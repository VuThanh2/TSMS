import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
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
  'User.CannotDeactivateSelf': 'Không thể vô hiệu hóa tài khoản của chính bạn.',
  'Lecturer.HasActiveCourses': 'Giảng viên này đang có khóa học đang hoạt động.',
  'Student.HasActiveEnrollment': 'Sinh viên này đang có đăng ký đang hoạt động.',
};

export function useCreateUser(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: createUserApi,
    onSuccess: () => {
      void message.success('Tạo người dùng thành công!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      const messages: Record<string, string> = {
        EmailAlreadyExists: 'Email đã tồn tại trong hệ thống.',
        InvalidRole: 'Role không hợp lệ.',
        PasswordPolicyViolation: 'Mật khẩu không đủ mạnh.',
      };
      void message.error(messages[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}

export function useEditUser(onSuccess?: () => void) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, ...data }: { userId: string; fullName: string; email: string; department?: string; major?: string }) =>
      updateUserApi(userId, data),
    onSuccess: () => {
      void message.success('Cập nhật thành công!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      onSuccess?.();
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}

export function useToggleUserStatus() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      updateUserStatusApi(userId, { isActive }),
    onSuccess: () => {
      void message.success('Cập nhật trạng thái thành công!');
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ code?: string; message?: string }>) => {
      const code = error.response?.data?.code ?? '';
      void message.error(STATUS_ERROR_MESSAGES[code] ?? error.response?.data?.message ?? 'Đã có lỗi xảy ra.');
    },
  });
}

export function useImportCsv() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: importUsersCsvApi,
    onSuccess: (res) => {
      const { successCount, failureCount } = res.data;
      if (failureCount === 0) {
        void message.success(`Import thành công ${successCount} người dùng!`);
      } else {
        void message.warning(`Thành công: ${successCount}, Thất bại: ${failureCount}`);
      }
      void queryClient.invalidateQueries({ queryKey: ['users'] });
    },
    onError: (error: AxiosError<{ message?: string }>) => {
      void message.error(error.response?.data?.message ?? 'Lỗi khi import CSV.');
    },
  });
}
