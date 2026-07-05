import { useState } from 'react';
import { Table, Input, Button, Tag, Popconfirm } from 'antd';
import { PlusOutlined, SearchOutlined, UploadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';

import type { UserListItem } from '@/modules/identity/shared/user.types';
import { useUserList, useToggleUserStatus } from './useUserList';
import CreateUserModal from './CreateUserModal';
import EditUserModal from './EditUserModal';
import ImportCsvModal from './ImportCsvModal';

const ROLE_OPTIONS = ['', 'Admin', 'Lecturer', 'Student'] as const;
const ROLE_LABELS: Record<string, string> = { '': 'All', Admin: 'Admin', Lecturer: 'Lecturer', Student: 'Student' };

export default function UserListPage() {
  const list = useUserList();
  const toggleStatus = useToggleUserStatus();

  const [createOpen, setCreateOpen] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [editUserId, setEditUserId] = useState<string | null>(null);

  const columns: ColumnsType<UserListItem> = [
    {
      title: 'Name',
      key: 'name',
      render: (_, record) => {
        const initials = record.fullName.split(' ').map((w) => w[0]).join('').slice(0, 2).toUpperCase();
        return (
          <div className="flex items-center gap-2.5">
            <div className="flex h-8 w-8 flex-none items-center justify-center rounded-full bg-bg-card text-[12px] font-semibold text-text-secondary">
              {initials}
            </div>
            <span className="text-[15px] font-semibold">{record.fullName}</span>
          </div>
        );
      },
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      render: (email: string) => <span className="font-mono text-[14px] text-text-secondary">{email}</span>,
    },
    {
      title: 'Role',
      dataIndex: 'role',
      key: 'role',
      render: (role: string) => (
        <Tag style={{ borderRadius: 9999, background: '#F3EEE9', color: '#5C5854', border: 'none', fontWeight: 600 }}>
          {role}
        </Tag>
      ),
    },
    {
      title: 'Status',
      dataIndex: 'isActive',
      key: 'status',
      render: (isActive: boolean) => (
        <Tag
          style={{
            borderRadius: 9999,
            border: 'none',
            fontWeight: 600,
            background: isActive ? '#D6F0E5' : '#FDE2E2',
            color: isActive ? '#1E875F' : '#D7372C',
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
          }}
        >
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: isActive ? '#1E875F' : '#D7372C' }} />
          {isActive ? 'Active' : 'Inactive'}
        </Tag>
      ),
    },
    {
      title: 'Actions',
      key: 'actions',
      align: 'right',
      render: (_, record) => (
        <div className="flex justify-end gap-1.5">
          <Button size="small" onClick={() => setEditUserId(record.userId)}>
            Edit
          </Button>
          <Popconfirm
            title={record.isActive ? 'Deactivate this user?' : 'Activate this user?'}
            description={record.isActive ? 'User sẽ không thể đăng nhập.' : 'User sẽ được phép đăng nhập trở lại.'}
            onConfirm={() => toggleStatus.mutate({ userId: record.userId, isActive: !record.isActive })}
            okText={record.isActive ? 'Deactivate' : 'Activate'}
            cancelText="Cancel"
            okButtonProps={{ danger: record.isActive }}
          >
            <Button size="small" danger={record.isActive}>
              {record.isActive ? 'Deactivate' : 'Activate'}
            </Button>
          </Popconfirm>
        </div>
      ),
    },
  ];

  return (
    <div className="p-10 px-12">
      {/* Header */}
      <div className="mb-7 flex items-start justify-between gap-4">
        <div>
          <h1 className="m-0 mb-1.5 text-[32px] font-bold tracking-tight">Users</h1>
          <p className="m-0 text-[15px] text-text-secondary">
            {list.totalCount} user{list.totalCount !== 1 ? 's' : ''} total
          </p>
        </div>
        <div className="flex gap-2">
          <Button icon={<UploadOutlined />} size="large" onClick={() => setImportOpen(true)}>
            Import CSV
          </Button>
          <Button type="primary" icon={<PlusOutlined />} size="large" onClick={() => setCreateOpen(true)}>
            Create user
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="mb-5 flex flex-wrap gap-3">
        <Input
          placeholder="Search by name or email…"
          prefix={<SearchOutlined />}
          value={list.search}
          onChange={(e) => {
            list.setSearch(e.target.value);
            list.setPage(1);
          }}
          allowClear
          className="min-w-[240px] flex-1"
          size="large"
        />
        <div className="flex gap-1.5 rounded-lg border border-border bg-white p-1">
          {ROLE_OPTIONS.map((r) => (
            <button
              key={r}
              onClick={() => {
                list.setRole(r);
                list.setPage(1);
              }}
              className={`cursor-pointer rounded-md border-none px-3 py-1.5 text-[13px] font-semibold transition-colors ${
                list.role === r
                  ? 'bg-primary text-white'
                  : 'bg-transparent text-text-muted hover:text-text'
              }`}
            >
              {ROLE_LABELS[r]}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <Table<UserListItem>
        columns={columns}
        dataSource={list.users}
        rowKey="userId"
        loading={list.isLoading}
        pagination={{
          current: list.page,
          pageSize: list.pageSize,
          total: list.totalCount,
          showSizeChanger: true,
          onChange: (p, ps) => {
            list.setPage(p);
            list.setPageSize(ps);
          },
        }}
      />

      <CreateUserModal open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditUserModal userId={editUserId} open={!!editUserId} onClose={() => setEditUserId(null)} />
      <ImportCsvModal open={importOpen} onClose={() => setImportOpen(false)} />
    </div>
  );
}
