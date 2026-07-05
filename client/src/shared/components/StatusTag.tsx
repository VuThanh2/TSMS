import { Tag } from 'antd';

type CourseStatus = 'Upcoming' | 'Active' | 'Completed';

const STATUS_CONFIG: Record<CourseStatus, { color: string; bg: string }> = {
  Upcoming: { color: '#2E73C4', bg: '#DBE8F7' },
  Active: { color: '#1E875F', bg: '#D6F0E5' },
  Completed: { color: '#5C5854', bg: '#F3EEE9' },
};

export default function StatusTag({ status }: { status: CourseStatus }) {
  const config = STATUS_CONFIG[status];
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
