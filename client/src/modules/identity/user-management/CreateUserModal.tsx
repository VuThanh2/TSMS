import { Modal, Form, Input, Radio } from 'antd';

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
    form.validateFields().then(({ confirmPassword: _confirmPassword, ...values }) => {
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
      destroyOnHidden
      width={480}
    >
      <Form form={form} layout="vertical" requiredMark={false} className="mt-4">
        <Form.Item
          label="Full name"
          name="fullName"
          rules={[{ required: true, message: 'Please enter the full name' }]}
        >
          <Input placeholder="e.g. John Smith" />
        </Form.Item>

        <Form.Item
          label="Email"
          name="email"
          rules={[
            { required: true, message: 'Please enter the email' },
            { type: 'email', message: 'Invalid email' },
          ]}
        >
          <Input placeholder="john.smith@university.edu" />
        </Form.Item>

        <Form.Item
          label="Role"
          name="role"
          rules={[{ required: true, message: 'Select a role' }]}
        >
          <Radio.Group
            optionType="button"
            buttonStyle="solid"
            className="flex gap-2 [&_.ant-radio-button-wrapper]:rounded-lg [&_.ant-radio-button-wrapper]:border-border-input [&_.ant-radio-button-wrapper]:before:hidden"
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
            { required: true, message: 'Please enter a password' },
            { min: 6, message: 'Password must be at least 6 characters' },
          ]}
          hasFeedback
        >
          <Input.Password placeholder="At least 6 characters" />
        </Form.Item>

        <Form.Item
          label="Confirm password"
          name="confirmPassword"
          dependencies={['password']}
          hasFeedback
          rules={[
            { required: true, message: 'Please re-enter the password' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('password') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('The confirmation password does not match'));
              },
            }),
          ]}
        >
          <Input.Password placeholder="Re-enter the password" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
