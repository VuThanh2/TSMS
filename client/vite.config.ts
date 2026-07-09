import path from 'node:path';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: true, // lắng nghe trên 0.0.0.0, bắt buộc để ngrok forward request vào được
    allowedHosts: ['.ngrok-free.dev', '.ngrok-free.app'], // cho phép mọi subdomain ngrok (cả 2 đuôi cũ/mới)
    proxy: {
      // Gộp Frontend + Backend qua 1 origin duy nhất -> chỉ cần 1 tunnel ngrok (Free plan giới hạn 1 endpoint)
      '/api': {
        // Port 7012 chỉ nghe HTTPS bằng self-signed dev cert (dotnet dev-certs) — secure:false
        // để Node bỏ qua bước verify CA, nếu không proxy sẽ reject rồi trả 502.
        target: 'https://localhost:7012',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'https://localhost:7012',
        changeOrigin: true,
        secure: false,
        ws: true, // bắt buộc cho SignalR (WebSocket), thiếu cờ này sẽ chỉ proxy được HTTP handshake rồi rớt kết nối realtime
      },
    },
  },
});
