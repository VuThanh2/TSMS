import { useEffect } from 'react';
import { Modal, Form, Input, Spin } from 'antd';

import { useUserDetail, useEditUser } from './useUserList';

interface EditUserModalProps {
  userId: string | null;
  open: boolean;
  onClose: () => void;
}

export default function EditUserModal({ userId, open, onClose }: EditUserModalProps) {
  const [form] = Form.useForm();
  const { data: user, isLoading } = useUserDetail(open ? userId : null);
  const { mutate, isPending } = useEditUser(() => onClose());

  // Prefill form khi user data load xong
  useEffect(() => {
    if (user) {
      form.setFieldsValue({
        fullName: user.fullName,
        email: user.email,
        department: user.profile?.department,
        major: user.profile?.major,
      });
    }
  }, [user, form]);

  function handleOk() {
    if (!userId) return;
    form.validateFields().then((values) => {
      mutate({ userId, ...values });
    });
  }

  return (
    <Modal
      title="Edit user"
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      confirmLoading={isPending}
      okText="Save"
      destroyOnClose
      width={480}
    >
      {isLoading ? (
        <div className="flex justify-center py-8"><Spin /></div>
      ) : (
        <Form form={form} layout="vertical" requiredMark={false} className="mt-4">
          <Form.Item
            label="Full name"
            name="fullName"
            rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: 'Vui lòng nhập email' },
              { type: 'email', message: 'Email không hợp lệ' },
            ]}
          >
            <Input />
          </Form.Item>

          {/* Profile field tuỳ theo role */}
          {user?.role === 'Lecturer' && (
            <Form.Item label="Department" name="department">
              <Input placeholder="e.g. Khoa Công nghệ Thông tin" />
            </Form.Item>
          )}
          {user?.role === 'Student' && (
            <Form.Item label="Major" name="major">
              <Input placeholder="e.g. Kỹ thuật Phần mềm" />
            </Form.Item>
          )}

          {/* Role read-only */}
          <div className="flex items-center gap-2.5 rounded-lg bg-[#F3EEE9] px-3.5 py-3">
            <span className="text-[14px] font-semibold text-text-muted">Role</span>
            <span className="text-[14px] font-semibold">{user?.role}</span>
            <span className="ml-auto text-[12px] text-text-muted">Read-only</span>
          </div>
        </Form>
      )}
    </Modal>
  );
}
