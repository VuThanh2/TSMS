import { Spin } from 'antd';

// Màn hình chờ dùng chung — thay cho việc render null (màn hình trắng) trong
// những khoảng app chưa sẵn sàng: đọc localStorage auth, chuyển route, F5 trang.
export default function LoadingScreen() {
  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-bg">
      <Spin size="large" />
    </div>
  );
}
