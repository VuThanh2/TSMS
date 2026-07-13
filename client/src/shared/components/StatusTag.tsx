import { Tag } from 'antd';

type StatusValue = 'Upcoming' | 'Active' | 'Completed' | 'Graded';

const STATUS_CONFIG: Record<StatusValue, { color: string; bg: string }> = {
  Upcoming: { color: '#2E73C4', bg: '#DBE8F7' },
  Active: { color: '#1E875F', bg: '#D6F0E5' },
  Completed: { color: '#5C5854', bg: '#F3EEE9' },
  Graded: { color: '#5A4BD1', bg: '#E7E4FB' },
};

const FALLBACK = { color: '#5C5854', bg: '#F3EEE9' };

export default function StatusTag({ status }: { status: StatusValue }) {
  // Fallback phòng khi status không khớp config (dữ liệu lạ) — tránh crash cả trang.
  const config = STATUS_CONFIG[status] ?? FALLBACK;
  return (
    <Tag
      style={{
        color: config.color,
        backgroundColor: config.bg,
        border: 'none',
        borderRadius: 9999,
        fontWeight: 600,
        fontSize: '12.5px',
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
      }}
    >
      <span
        style={{
          width: 6,
          height: 6,
          borderRadius: '50%',
          backgroundColor: config.color,
        }}
      />
      {status}
    </Tag>
  );
}
