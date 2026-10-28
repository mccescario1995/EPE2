import { ref } from "vue";

type ModalType = "save" | "delete" | "info" | "question" | "warning";

interface ModalOptions {
  title?: string;
  message?: string;
  type?: ModalType;
}

const isOpen = ref(false);
const message = ref("");
const type = ref<ModalType>("info");
const title = ref("Confirmation");

let resolveFn: ((confirmed: boolean) => void) | null = null;

/**
 * Composable for globally controlling the confirmation modal.
 */
export const useConfirmModal = () => {
  /**
   * Open the modal and return a Promise that resolves with the user action.
   * @param modalMessage The message displayed in the modal
   * @param modalType The type of modal: 'save' | 'delete' | 'info' | 'question' | 'warning'
   * @param modalTitle Optional title for the modal
   * @returns Promise<boolean> - true if confirmed, false if canceled
   */
  const open = ({
    message: modalMessage,
    type: modalType = "info",
    title: modalTitle = "Confirmation",
  }: ModalOptions): Promise<boolean> => {
    message.value = modalMessage ?? "";
    type.value = modalType;
    title.value = modalTitle;
    isOpen.value = true;

    return new Promise((resolve) => {
      resolveFn = resolve;
    });
  };

  const confirm = () => {
    isOpen.value = false;
    resolveFn?.(true);
  };

  const close = () => {
    isOpen.value = false;
    resolveFn?.(false);
  };

  return {
    isOpen,
    message,
    type,
    title,
    open,
    confirm,
    close,
  };
};
