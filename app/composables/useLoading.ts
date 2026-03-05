type spinnercolor =
  | "blue"
  | "purple"
  | "green"
  | "red"
  | "orange"
  | "pink"
  | "indigo"
  | "teal"
  | "cyan"
  | "gray"
  | 'black';

interface Loading {
  loadingText: string;
  subText: string | null;
  spinnerColor: spinnercolor;
  showEllipsis: boolean;
}

// Global loading state
const loading = ref(false);
const loadingConfig = ref<Loading>({
  loadingText: "Loading...",
  subText: null,
  spinnerColor: "blue",
  showEllipsis: true
});

export const useLoading = () => {
  const start = ({
    loadingText = "Loading...",
    subText = null,
    spinnerColor = "blue",
    showEllipsis = true,
  }: {
    loadingText?: string;
    subText?: string | null;
    spinnerColor?: spinnercolor;
    showEllipsis?: boolean;
  } = {}) => {
    loadingConfig.value = { loadingText, subText, spinnerColor, showEllipsis };
    loading.value = true;
  };
  
  const stop = () => {
    loading.value = false;
  };

  return {
    isLoading: loading,
    loadingConfig,
    startLoading: start,
    stopLoading: stop,
  };
};