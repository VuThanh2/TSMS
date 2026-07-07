import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider, App as AntdApp } from 'antd';

import { AuthProvider } from '@/shared/lib/auth-context';
import App from './App';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

// Theme warm palette khớp với design system (TSMS.dc.html)
const antdTheme = {
  token: {
    colorPrimary: '#F45D48',
    colorBgBase: '#FFF7F0',
    colorText: '#1C1B1A',
    colorTextSecondary: '#5C5854',
    colorBorder: '#E8DDD3',
    colorBgContainer: '#FFFFFF',
    fontFamily: "'Inter', -apple-system, sans-serif",
    borderRadius: 8,
  },
  components: {
    // Modal trong mock có border-radius 20px, padding 32px — khác token borderRadius chung (8px)
    Modal: {
      borderRadiusLG: 20,
      padding: 32,
      titleFontSize: 22,
    },
  },
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <ConfigProvider theme={antdTheme}>
            <AntdApp>
              <App />
            </AntdApp>
          </ConfigProvider>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
