import { Modal, Upload, Table, Button } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';

import { useImportCsv } from './useUserList';

interface ImportCsvModalProps {
  open: boolean;
  onClose: () => void;
}

// Nguồn sự thật DUY NHẤT cho định dạng CSV import — vừa hiển thị hint, vừa sinh
// file template. Phải khớp đúng thứ tự cột backend đọc (ImportUsersCsvCommand:
// FullName, Email, Role, Password) và dùng delimiter dấu phẩy vì CsvHelper cấu
// hình InvariantCulture.
const CSV_COLUMNS = ['FullName', 'Email', 'Role', 'Password'] as const;
const CSV_SAMPLE_ROWS = [
  ['Nguyen Van A', 'vana@example.com', 'Student', 'Passw0rd!'],
  ['Tran Thi B', 'thib@example.com', 'Lecturer', 'Passw0rd!'],
];

// Tạo file CSV ngay ở client rồi trigger download — template là dữ liệu tĩnh,
// không cần endpoint backend. Prefix BOM (﻿) để Excel mở đúng UTF-8.
function downloadCsvTemplate() {
  const lines = [CSV_COLUMNS.join(','), ...CSV_SAMPLE_ROWS.map((r) => r.join(','))];
  const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = 'user-import-template.csv';
  link.click();
  URL.revokeObjectURL(url);
}

export default function ImportCsvModal({ open, onClose }: ImportCsvModalProps) {
  const { mutate, data, isPending, reset } = useImportCsv();

  const result = data?.data;

  function handleClose() {
    reset();
    onClose();
  }

  return (
    <Modal
      title="Import users from CSV"
      open={open}
      onCancel={handleClose}
      footer={null}
      destroyOnHidden
      width={560}
    >
      <Upload.Dragger
        accept=".csv"
        maxCount={1}
        showUploadList={false}
        customRequest={({ file }) => {
          mutate(file as File);
        }}
        disabled={isPending}
        className="!bg-[#FFFCF9] !border-border-input"
      >
        <p className="ant-upload-text mb-1 text-[15px] font-semibold">
          {isPending ? 'Importing…' : 'Drop a CSV file here'}
        </p>
        <p className="ant-upload-hint text-[13px] text-text-muted">
          Columns: {CSV_COLUMNS.join(', ')}
        </p>
      </Upload.Dragger>

      {/* Tải file mẫu — user điền theo đúng định dạng rồi upload lại */}
      <div className="mt-3 flex items-center justify-between text-[13px] text-text-muted">
        <span>Not sure about the format?</span>
        <Button type="link" size="small" icon={<DownloadOutlined />} onClick={downloadCsvTemplate} className="px-0">
          Download template
        </Button>
      </div>

      {/* Kết quả import */}
      {result && (
        <div className="mt-4">
          <div className="mb-3 flex justify-between rounded-lg bg-bg-card px-4 py-3 text-[13px] font-semibold">
            <span>Results</span>
            <span>
              <span className="text-[#1E875F]">{result.successCount} succeeded</span>
              {result.failureCount > 0 && (
                <span className="text-[#D7372C]"> · {result.failureCount} failed</span>
              )}
            </span>
          </div>

          {result.errors.length > 0 && (
            <Table
              size="small"
              dataSource={result.errors}
              rowKey="rowNumber"
              pagination={false}
              columns={[
                {
                  title: 'Row',
                  dataIndex: 'rowNumber',
                  key: 'rowNumber',
                  width: 60,
                  render: (v: number) => <span className="font-mono font-semibold text-text-muted">{v}</span>,
                },
                {
                  title: 'Error',
                  dataIndex: 'reason',
                  key: 'reason',
                  render: (v: string) => <span className="text-[#D7372C]">{v}</span>,
                },
              ]}
            />
          )}
        </div>
      )}
    </Modal>
  );
}
