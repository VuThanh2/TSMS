import { Modal, Form, Input, InputNumber, DatePicker } from 'antd';

import LecturerPicker from '@/modules/course-management/shared/LecturerPicker';
import { useCreateCourse } from './useCreateCourse';

interface CreateCourseModalProps {
  open: boolean;
  onClose: () => void;
}

interface CreateCourseFormValues {
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  lecturerId: string;
}

export default function CreateCourseModal({ open, onClose }: CreateCourseModalProps) {
  const [form] = Form.useForm<CreateCourseFormValues>();
  const { mutate, isPending } = useCreateCourse(() => {
    form.resetFields();
    onClose();
  });

  function handleOk() {
    form.validateFields().then((values) => {
      mutate({
        ...values,
        // DatePicker trả dayjs object, convert sang ISO string
        startDate: typeof values.startDate === 'string' ? values.startDate : (values.startDate as unknown as { format: (f: string) => string }).format('YYYY-MM-DD'),
        endDate: typeof values.endDate === 'string' ? values.endDate : (values.endDate as unknown as { format: (f: string) => string }).format('YYYY-MM-DD'),
      });
    });
  }

  return (
    <Modal
      title="Create course"
      open={open}
      onOk={handleOk}
      onCancel={onClose}
      confirmLoading={isPending}
      okText="Create"
      cancelText="Cancel"
      width={520}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" requiredMark={false} className="mt-4">
        <Form.Item
          label="Course name"
          name="name"
          rules={[{ required: true, message: 'Please enter the course name' }]}
        >
          <Input placeholder="e.g. Databases" />
        </Form.Item>

        <Form.Item label="Description" name="description">
          <Input.TextArea placeholder="Short description of the course…" rows={3} className="resize-none" />
        </Form.Item>

        <div className="grid grid-cols-2 gap-3">
          <Form.Item
            label="Start date"
            name="startDate"
            rules={[{ required: true, message: 'Select a start date' }]}
          >
            <DatePicker className="w-full" />
          </Form.Item>
          <Form.Item
            label="End date"
            name="endDate"
            rules={[{ required: true, message: 'Select an end date' }]}
          >
            <DatePicker className="w-full" />
          </Form.Item>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Form.Item
            label="Max capacity"
            name="maxCapacity"
            rules={[{ required: true, message: 'Enter the maximum capacity' }]}
          >
            <InputNumber min={1} placeholder="30" className="w-full font-mono" />
          </Form.Item>
          <Form.Item
            label="Lecturer"
            name="lecturerId"
            rules={[{ required: true, message: 'Select a lecturer' }]}
          >
            <LecturerPicker />
          </Form.Item>
        </div>
      </Form>
    </Modal>
  );
}
