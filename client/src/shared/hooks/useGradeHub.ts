import { useEffect, useRef } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createGradeHubConnection } from '@/shared/lib/signalr';

export function useGradeHub(onGradeUpdated: (payload: unknown) => void) {
  const connectionRef = useRef<HubConnection | null>(null);

  // "Latest ref" pattern: luôn cập nhật ref mỗi render (dòng dưới chạy ở mọi render,
  // không phải trong useEffect), nhưng KHÔNG đưa onGradeUpdated vào dependency array
  // của useEffect bên dưới. Nhờ vậy effect chỉ chạy đúng 1 lần lúc mount (connect 1 lần
  // duy nhất), trong khi handler bên trong vẫn luôn gọi đúng callback MỚI NHẤT qua ref
  // — không bị "đóng băng" theo closure của lần render đầu tiên.
  const handlerRef = useRef(onGradeUpdated);
  handlerRef.current = onGradeUpdated;

  useEffect(() => {
    const connection = createGradeHubConnection();
    connectionRef.current = connection;

    const handleGradeUpdated = (payload: unknown) => handlerRef.current(payload);
    connection.on('GradeUpdated', handleGradeUpdated);

    connection.onclose(() => {
      console.warn('GradeHub connection đã đóng — có thể do token hết hạn.');
    });

    connection.start().catch((error: unknown) => {
      console.error('Không thể kết nối GradeHub:', error);
    });

    return () => {
      connection.off('GradeUpdated', handleGradeUpdated);
      void connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- cố ý chỉ chạy 1 lần lúc mount,
    // callback mới nhất đã được đảm bảo qua handlerRef ở trên, không cần liệt kê ở đây.
  }, []);
}