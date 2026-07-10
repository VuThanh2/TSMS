import React, { useState } from 'react';
import { Calendar, Button } from 'antd';
import { LeftOutlined, RightOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';

// Calendar dùng chung cho lịch Lecturer/Student — điều hướng theo từng tháng
// (prev/next) thay vì hiển thị toàn bộ kỳ. Nhận sẵn map ngày -> danh sách buổi
// và hàm render chip để mỗi trang tự quyết định hiển thị buổi học thế nào.
interface ScheduleCalendarProps<T> {
  sessionsByDate: Record<string, T[]>;
  getKey: (item: T) => string;
  renderChip: (item: T) => React.ReactNode;
}

export default function ScheduleCalendar<T>({
  sessionsByDate,
  getKey,
  renderChip,
}: ScheduleCalendarProps<T>) {
  // Tách rõ 2 khái niệm mà antd Calendar vốn gộp chung vào `value`:
  //  - panel: tháng ĐANG XEM (prev/next điều khiển).
  //  - selected: NGÀY đã bấm chọn — độc lập với panel để khi chuyển tháng, ô chọn
  //    KHÔNG bị kéo sang ngày cùng số ở tháng khác; quay lại đúng tháng mới hiện lại.
  const [panel, setPanel] = useState<Dayjs>(dayjs());
  const [selected, setSelected] = useState<Dayjs | null>(null);

  // Chặn chọn ngày thuộc tháng trước/sau — đây là nguyên nhân Calendar tự đổi
  // tháng khi bấm vào ô đầu/cuối lưới (antd Calendar không tôn trọng hoàn toàn
  // controlled value khi cell được select, chỉ disabledDate mới chặn được click).
  function disabledDate(current: Dayjs) {
    return !current.isSame(panel, 'month');
  }

  // DÙNG fullCellRender (không phải cellRender): antd v6 với cellRender sẽ tự bọc
  // output trong .ant-picker-calendar-date (element bị tô nền "selected" theo
  // value=panel) và tự render số ngày → gây highlight nhầm khi chuyển tháng + trùng số.
  // fullCellRender trả thẳng nội dung mình dựng, antd KHÔNG bọc/tô gì → highlight ngày
  // chọn hoàn toàn do mình kiểm soát, chỉ bám đúng ngày thật.
  //
  // Nội dung ô được dựng lại BÁM SÁT look gốc của antd fullscreen calendar (số ngày
  // 2 chữ số căn phải, viền trên phân cách ô, hôm nay = viền trên primary, ngày chọn
  // = nền primary nhạt + số primary) để giữ đúng UI cũ mà người dùng thích.
  function renderCell(
    current: Dayjs,
    info: { type: string; originNode: React.ReactElement },
  ) {
    if (info.type !== 'date') return info.originNode;

    // Ô thuộc tháng trước/sau — để trống cho dễ nhìn
    if (!current.isSame(panel, 'month')) return <div className="h-full" />;

    const dateStr = current.format('YYYY-MM-DD');
    const daySessions = sessionsByDate[dateStr] ?? [];
    const isSelected = selected != null && current.isSame(selected, 'day');
    const isToday = current.isSame(dayjs(), 'day');

    // Nền ô: ngày chọn đậm nhất, hôm nay nền primary rất nhạt để cũng nổi hơn ngày thường
    const cellBg = isSelected
      ? 'rgba(244, 93, 72, 0.18)'
      : isToday
        ? 'rgba(244, 93, 72, 0.07)'
        : undefined;

    // Badge số ngày: ngày thường để phẳng; Today + ngày chọn cho vào badge tròn nổi bật.
    // - selected: tròn nền primary đặc, số trắng (nổi nhất)
    // - today:    tròn viền primary 2px, số primary đậm
    const badge = isSelected
      ? 'bg-primary font-bold text-white shadow-sm'
      : isToday
        ? 'border-2 border-primary font-bold text-primary'
        : 'font-normal text-text';
    const isBadge = isSelected || isToday;

    return (
      <div
        className={`flex h-full flex-col transition-colors ${isSelected ? '' : 'hover:bg-[rgba(0,0,0,0.03)]'}`}
        style={{
          margin: '0 4px',
          minHeight: 96,
          padding: '4px 8px 0',
          // Viền trên phân cách ô — hôm nay tô primary đậm (đúng như antd gốc)
          borderTop: `2px solid ${isToday ? '#F45D48' : 'var(--color-border)'}`,
          background: cellBg,
        }}
      >
        <div className="flex justify-end">
          <span
            className={`inline-flex h-7 items-center justify-center rounded-full text-[14px] ${badge}`}
            style={{ minWidth: 28, paddingInline: isBadge ? 6 : 0, lineHeight: '24px' }}
          >
            {String(current.date()).padStart(2, '0')}
          </span>
        </div>
        {daySessions.length > 0 && (
          <ul className="m-0 flex-1 list-none overflow-y-auto p-0 pb-0.5 text-left">
            {daySessions.map((s) => (
              <li key={getKey(s)}>{renderChip(s)}</li>
            ))}
          </ul>
        )}
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded-xl border border-border bg-white shadow-sm">
      <Calendar
        value={panel}
        onSelect={(date) => {
          // Bấm 1 ngày trong panel → ghi nhận ngày chọn; giữ panel ở đúng tháng đó.
          setSelected(date);
          setPanel(date);
        }}
        disabledDate={disabledDate}
        fullCellRender={renderCell}
        // Header tự vẽ: chỉ điều hướng theo tháng, ẩn dropdown năm/tháng mặc định
        headerRender={() => (
          <div className="flex items-center justify-between border-b border-border px-5 py-3.5">
            <div className="text-[18px] font-bold capitalize tracking-tight">
              {panel.format('MMMM YYYY')}
            </div>
            <div className="flex items-center gap-2">
              <Button
                icon={<LeftOutlined />}
                onClick={() => setPanel(panel.clone().subtract(1, 'month'))}
                aria-label="Previous month"
              />
              <Button onClick={() => setPanel(dayjs())}>Today</Button>
              <Button
                icon={<RightOutlined />}
                onClick={() => setPanel(panel.clone().add(1, 'month'))}
                aria-label="Next month"
              />
            </div>
          </div>
        )}
      />
    </div>
  );
}
