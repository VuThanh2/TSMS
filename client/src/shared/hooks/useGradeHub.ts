import { useEffect, useRef } from 'react';
import type { HubConnection } from '@microsoft/signalr';
import { createGradeHubConnection } from '@/shared/lib/signalr';

export function useGradeHub(onGradeUpdated: (payload: unknown) => void) {
  const connectionRef = useRef<HubConnection | null>(null);

  // "Latest ref" pattern: callback ref luôn trỏ đến phiên bản mới nhất mà không
  // cần đưa hàm vào dependency array, tránh connect/disconnect mỗi lần render.
  const handlerRef = useRef(onGradeUpdated);
  handlerRef.current = onGradeUpdated;

  useEffect(() => {
    const connection = createGradeHubConnection();
    connectionRef.current = connection;

    // isCancelled ngăn log lỗi khi React Strict Mode chạy cleanup trước khi
    // negotiate xong — stop() gọi trong cleanup làm connection.start() reject
    // với "stopped during negotiation", đây là hành vi bình thường trong dev.
    let isCancelled = false;

    const handleGradeUpdated = (payload: unknown) => handlerRef.current(payload);
    connection.on('GradeUpdated', handleGradeUpdated);

    connection.onclose(() => {
      if (!isCancelled) {
        console.warn('GradeHub connection đã đóng — có thể do token hết hạn.');
      }
    });

    connection.start().catch((error: unknown) => {
      if (isCancelled) return;
      const msg = error instanceof Error ? error.message : String(error);
      // "stopped during negotiation" luôn do cleanup() gọi stop() — không phải lỗi thật.
      if (msg.includes('stopped during negotiation')) return;
      console.error('Không thể kết nối GradeHub:', error);
    });

    return () => {
      isCancelled = true;
      connection.off('GradeUpdated', handleGradeUpdated);
      void connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional: only mount once,
    // latest callback is captured via handlerRef above.
  }, []);
}
