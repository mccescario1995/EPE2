import { reactive } from "vue";

type ToastType = "success" | "error" | "info" | "warning";

type ToastPosition =
  | "top-right"
  | "top-left"
  | "bottom-right"
  | "bottom-left"
  | "top-center"
  | "bottom-center";

  type AnimationType = 
  | 'shake' 
  | 'bounce' 
  | 'flip' 
  | 'zoom' 
  | 'swing' 
  | 'tada' 
  | 'wobble' 
  | 'pulse'
  | 'none';

interface Toast {
  id: number;
  title: string;
  message: string;
  type: ToastType;
  animation: AnimationType;
  duration: number;
  position: ToastPosition;
}

const toasts = reactive<Toast[]>([]);

let idCounter = 0;

/**
 * Add a toast with customizable properties
 */
export const useToast = () => {
  const showToast = ({
    title,
    message,
    type = "info",
    animation = 'zoom',
    duration = 3000,
    position = "top-right", // ✅ New position option
  }: {
    title: string;
    message: string;
    type?: ToastType;
    animation?: AnimationType;
    duration?: number;
    position?: ToastPosition;
  }) => {
    // ✅ Clear any existing toasts before showing new one
    //toasts.splice(0, toasts.length);

    const id = ++idCounter;
    const toast: Toast = { id, title, message, type, animation,duration, position };
    toasts.push(toast);

    setTimeout(() => {
      const idx = toasts.findIndex((t) => t.id === id);
      if (idx !== -1) toasts.splice(idx, 1);
    }, duration);
  };

  return { showToast };
};

/**
 * Global reactive access for Toast.vue
 */
export const useToastStore = () => {
  const removeToast = (id: number) => {
    const index = toasts.findIndex((t) => t.id === id);
    if (index !== -1) toasts.splice(index, 1);
  };

  return {
    toasts,
    removeToast,
  };
};
