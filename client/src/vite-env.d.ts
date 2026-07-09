/// <reference types="vite/client" />

// Khai báo type tường minh cho các biến VITE_* — nếu không có phần này,
// import.meta.env.VITE_API_BASE_URL sẽ có type "any", mất luôn lợi ích
// type-safety của TypeScript ngay tại nơi quan trọng nhất (URL gọi API).
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_SIGNALR_HUB_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}