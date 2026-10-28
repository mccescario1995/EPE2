import { ref } from "vue";
import { decryptStorage } from "../utils/storageCrypto";

const basicRequirements = reactive({});
const showMessageDialog = ref(false);
const formDataCustomer = ref(new FormData());
const clearedFormData = new FormData();
const toastMessage = ref();
const toastType = ref();
const toastOpen = ref();
const hoveredTooltip = ref(null);
const openDialog = ref(false);
const dialogType = ref("");
const dialogMessage = ref("");
const quillIsReadOnly = ref(false);
const isEdit = ref(false);
const customerStatus = ref(1);
const toast = reactive({
  message: "",
  type: "",
  title: "",
  toggle: false,
});
const { showToast } = useToast();
const { startLoading, stopLoading } = useLoading();
const confirmModal = useConfirmModal();

const userDetails = ref(
  localStorage.getItem("user-details")
    ? decryptStorage<any>(localStorage.getItem("user-details"))?.objParam1 ?? {}
    : {}
);

export {
  basicRequirements,
  showMessageDialog,
  formDataCustomer,
  toastMessage,
  toastOpen,
  toastType,
  hoveredTooltip,
  openDialog,
  dialogMessage,
  dialogType,
  quillIsReadOnly,
  toast,
  showToast,
  startLoading,
  stopLoading,
  isEdit,
  confirmModal,
  customerStatus,
  userDetails,
};
