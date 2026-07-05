// Envelope chuẩn khớp với Result<T> bên Backend
export interface ApiResult<T> {
  isSuccess: boolean;
  value?: T;
  error?: ApiError;
}

export interface ApiError {
  code: string;
  message: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
