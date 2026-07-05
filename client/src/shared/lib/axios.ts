import axios from 'axios';

const TOKEN_STORAGE_KEY = 'accessToken';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Gắn Bearer token cho mọi request (trừ login/reset-password do Backend AllowAnonymous)
api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Xử lý response lỗi: 401 → hết phiên, 403 → không đủ quyền
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
      // Redirect về /login — dùng window.location thay vì navigate vì interceptor
      // nằm ngoài React tree, không truy cập được router context
      window.location.href = '/login';
    }
    return Promise.reject(error);
  },
);

export default api;
