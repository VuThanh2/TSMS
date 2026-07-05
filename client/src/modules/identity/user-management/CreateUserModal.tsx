import { Modal, Form, Input, Select } from 'antd';

import { useCreateUser } from './useUserList';

interface CreateUserModalProps {
  open: boolean;
  onClose: () => void;
}

export default function CreateUserModal({ open, onClose }: CreateUserModalProps) {
  const [form] = Form.useForm();
  const { mutate, isPending } = useCreateUser(() => {
    form.resetFields();
    onClose();
  });

  function handleOk() {
    form.validateFields().then((values) => {
      mutate(values);
    });
  }

  return (
    <Modal
      title="Create user"
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      confirmLoading={isPending}
      okText="Create"
      destroyOnClose
      width={480}
    >
      <Form form={form} layout="vertical" requiredMark={false} className="mt-4">
        <Form.Item
          label="Full name"
          name="fullName"
          rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}
        >
          <Input placeholder="e.g. Nguyễn Văn An" />
        </Form.Item>

        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Vui lòng nhập email' },
            { type: 'email', message: 'Email không hợp lệ' },
          ]}
        >
          <Input placeholder="an.nguyen@university.edu" />
        </Form.Item>

        <Form.Item
          label="Role"
          name="role"
          rules={[{ required: true, message: 'Chọn role' }]}
        >
          <Select
            placeholder="Select role"
            options={[
              { value: 'Admin', label: 'Admin' },
              { value: 'Lecturer', label: 'Lecturer' },
              { value: 'Student', label: 'Student' },
            ]}
          />
        </Form.Item>

        <Form.Item
          label="Password"
          name="password"
          rules={[
            { required: true, message: 'Vui lòng nhập mật khẩu' },
            { min: 6, message: 'Mật khẩu phải có ít nhất 6 ký tự' },
          ]}
        >
          <Input.Password placeholder="Tối thiểu 6 ký tự" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
