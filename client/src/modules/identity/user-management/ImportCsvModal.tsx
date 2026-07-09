import { Modal, Upload, Table } from 'antd';

import { useImportCsv } from './useUserList';

interface ImportCsvModalProps {
  open: boolean;
  onClose: () => void;
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
          Columns: FullName, Email, Role, Password
        </p>
      </Upload.Dragger>

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
