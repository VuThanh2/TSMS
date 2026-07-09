import api from '@/shared/lib/axios';

// POST /api/auth/logout — cần Bearer token, trả 204
// Chỉ ghi audit log phía server, không revoke token (stateless JWT)
export function logoutApi() {
  return api.post('/auth/logout');
}
