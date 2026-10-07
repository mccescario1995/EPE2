<template>
  <button
    class="relative inline-flex items-center justify-center font-medium rounded-lg transition-all duration-300 focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed overflow-hidden group"
    :class="[
      sizeClasses[size],
      variantClasses[variant],
      loading ? 'pointer-events-none' : '',
      customClass,
    ]"
    :disabled="disabled || loading"
    @click="handleClick"
  >
    <!-- Ripple effect background -->
    <span
      v-if="!variant.includes('outline') && !variant.includes('ghost')"
      class="absolute inset-0 bg-white/20 translate-y-full group-hover:translate-y-0 transition-transform duration-300 ease-out"
    ></span>

    <!-- Shine effect -->
    <span
      class="absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-500"
      :class="shineEffect"
    ></span>

    <!-- Content wrapper -->
    <span class="relative flex items-center justify-center">
      <!-- Loading spinner -->
      <span v-if="loading" class="mr-2 animate-spin">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24">
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          ></path>
        </svg>
      </span>

      <!-- Added "center"  : mccesccario -->
      <!-- Icon -->
      <span
        v-if="icon && !loading"
        class="transition-transform duration-300 group-hover:scale-110"
        :class="
          iconPosition === 'right'
            ? 'order-2 mx-auto md:ml-2'
            : iconPosition === 'center'
              ? 'mx-auto'
              : 'mx-auto md:mr-2'
        "
      >
        <component
          v-if="
            typeof icon === 'string' && iconComponents[getIconComponent(icon)]
          "
          :is="iconComponents[getIconComponent(icon)]"
          :class="iconSizeClasses[size]"
        />
        <component
          v-else-if="typeof icon === 'object' || typeof icon === 'function'"
          :is="icon"
          :class="iconSizeClasses[size]"
        />
      </span>

      <!-- Slot content -->
      <span
        class="transition-transform duration-300 group-hover:translate-x-0.5"
      >
        <slot></slot>
      </span>
    </span>

    <!-- Glow effect for primary buttons -->
    <span
      v-if="variant === 'primary' || variant === 'success'"
      class="absolute inset-0 opacity-0 group-hover:opacity-100 blur-xl transition-opacity duration-500 -z-10"
      :class="glowClasses[variant]"
    ></span>
  </button>
</template>

<script setup>
import { defineAsyncComponent } from "vue";

const props = defineProps({
  variant: {
    type: String,
    default: "primary",
    validator: (value) =>
      [
        "primary",
        "success",
        "danger",
        "warning",
        "info",
        "outline-primary",
        "outline-success",
        "outline-danger",
        "outline-warning",
        "outline-info",
        "ghost",
        "ghost-primary",
        "gradient",
        "dark:primary",
        "dark:success",
        "dark:danger",
        "dark:warning",
        "dark:info",
        "dark:outline-primary",
        "dark:outline-success",
        "dark:outline-danger",
        "dark:outline-warning",
        "dark:outline-info",
        "dark:ghost",
        "dark:ghost-primary",
        "dark:gradient",
      ].includes(value),
  },
  size: {
    type: String,
    default: "md",
    validator: (value) => ["xs", "sm", "md", "lg", "xl"].includes(value),
  },
  icon: {
    type: [String, Object, Function],
    default: null,
  },
  iconPosition: {
    type: String,
    default: "left",
    validator: (value) => ["left", "right", "center"].includes(value), // Added "center" option : mccescario
  },
  loading: {
    type: Boolean,
    default: false,
  },
  disabled: {
    type: Boolean,
    default: false,
  },
  customClass: {
    type: String,
    default: "",
  },
});

const emit = defineEmits(["onclick"]);

const handleClick = (event) => {
  if (props.loading || props.disabled) return;

  emit("onclick", event);
};

const sizeClasses = {
  xs: "px-3 py-1.5 text-xs gap-1.5",
  sm: "px-4 py-2 text-sm gap-2",
  md: "px-5 py-2.5 text-sm gap-2",
  lg: "px-6 py-3 text-base gap-2.5",
  xl: "px-8 py-4 text-lg gap-3",
};

const iconSizeClasses = {
  xs: "w-3.5 h-3.5",
  sm: "w-4 h-4",
  md: "w-4 h-4",
  lg: "w-5 h-5",
  xl: "w-6 h-6",
};

const variantClasses = {
  primary:
    "bg-blue-600 text-white hover:bg-blue-700 hover:shadow-lg hover:shadow-blue-500/50 active:scale-95 focus:ring-blue-500",
  success:
    "bg-green-600 text-white hover:bg-green-700 hover:shadow-lg hover:shadow-green-500/50 active:scale-95 focus:ring-green-500",
  danger:
    "bg-red-600 text-white hover:bg-red-700 hover:shadow-lg hover:shadow-red-500/50 active:scale-95 focus:ring-red-500",
  warning:
    "bg-yellow-600 text-white hover:bg-yellow-700 hover:shadow-lg hover:shadow-yellow-500/50 active:scale-95 focus:ring-yellow-500",
  info: "bg-gray-600 text-white hover:bg-gray-700 hover:shadow-lg hover:shadow-gray-500/50 active:scale-95 focus:ring-gray-500",
  "outline-primary":
    "border-2 border-blue-600 text-blue-600 hover:bg-blue-50 hover:border-blue-700 hover:shadow-md active:scale-95 focus:ring-blue-500 dark:bg-blue-600 dark:text-white dark:hover:bg-blue-700 dark:hover:shadow-lg dark:hover:shadow-blue-500/50 dark:active:scale-95 dark:focus:ring-blue-500",
  "outline-success":
    "border-2 border-green-600 text-green-600 hover:bg-green-50 hover:border-green-700 hover:shadow-md active:scale-95 focus:ring-green-500 dark:border-2 dark:border-green-600 dark:text-green-600 dark:hover:text-white dark:hover:bg-green-500 dark:hover:border-green-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-green-500",
  "outline-danger":
    "border-2 border-red-600 text-red-600 hover:bg-red-50 hover:border-red-700 hover:shadow-md active:scale-95 focus:ring-red-500 dark:bg-red-600 dark:text-white dark:hover:bg-red-700 dark:hover:shadow-lg dark:hover:shadow-red-500/50 dark:active:scale-95 dark:focus:ring-red-500",
  "outline-warning":
    "border-2 border-yellow-600 text-yellow-600 hover:bg-yellow-50 hover:border-yellow-700 hover:shadow-md active:scale-95 focus:ring-yellow-500 dark:border-2 dark:border-yellow-600 dark:text-yellow-600 dark:hover:text-white dark:hover:bg-yellow-500 dark:hover:border-yellow-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-yellow-500",
  "outline-info":
    "border-2 border-gray-600 text-gray-600 hover:bg-gray-50 hover:border-gray-700 hover:shadow-md active:scale-95 focus:ring-gray-500 dark:border-2 dark:border-gray-600 dark:text-white dark:hover:text-white dark:hover:bg-gray-500 dark:hover:border-gray-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-gray-500",
  ghost:
    "text-gray-700 hover:bg-gray-100 hover:text-gray-900 active:scale-95 focus:ring-gray-500",
  "ghost-primary":
    "text-blue-600 hover:bg-blue-50 hover:text-blue-700 active:scale-95 focus:ring-blue-500",
  gradient:
    "bg-gradient-to-r from-blue-600 to-indigo-600 text-white hover:from-blue-700 hover:to-indigo-700 hover:shadow-lg hover:shadow-blue-500/50 active:scale-95 focus:ring-blue-500",
  "dark:primary":
    "dark:bg-blue-600 dark:text-white dark:hover:bg-blue-700 dark:hover:shadow-lg dark:hover:shadow-blue-500/50 dark:active:scale-95 dark:focus:ring-blue-500",
  "dark:success":
    "dark:bg-green-600 dark:text-white dark:hover:bg-green-700 dark:hover:shadow-lg dark:hover:shadow-green-500/50 dark:active:scale-95 dark:focus:ring-green-500",
  "dark:danger":
    "dark:bg-red-600 dark:text-white dark:hover:bg-red-700 dark:hover:shadow-lg dark:hover:shadow-red-500/50 dark:active:scale-95 dark:focus:ring-red-500",
  "dark:warning":
    "dark:bg-yellow-600 dark:text-white dark:hover:bg-yellow-700 dark:hover:shadow-lg dark:hover:shadow-yellow-500/50 dark:active:scale-95 dark:focus:ring-yellow-500",
  "dark:info":
    "dark:bg-gray-600 dark:text-white dark:hover:bg-gray-700 dark:hover:shadow-lg dark:hover:shadow-gray-500/50 dark:active:scale-95 dark:focus:ring-gray-500",
  "dark:outline-primary":
    "dark:border-2 dark:border-blue-600 dark:text-blue-600 dark:hover:text-white dark:hover:bg-blue-500 dark:hover:border-blue-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-blue-500",
  "dark:outline-success":
    "dark:border-2 dark:border-green-600 dark:text-green-600 dark:hover:text-white dark:hover:bg-green-500 dark:hover:border-green-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-green-500",
  "dark:outline-danger":
    "dark:border-2 dark:border-red-600 dark:text-red-600 dark:hover:text-white dark:hover:bg-red-500 dark:hover:border-red-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-red-500",
  "dark:outline-warning":
    "dark:border-2 dark:border-yellow-600 dark:text-yellow-600 dark:hover:text-white dark:hover:bg-yellow-500 dark:hover:border-yellow-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-yellow-500",
  "dark:outline-info":
    "dark:border-2 dark:border-gray-600 dark:text-gray-600 dark:hover:text-white dark:hover:bg-gray-500 dark:hover:border-gray-700 dark:hover:shadow-md dark:active:scale-95 dark:focus:ring-gray-500",
  "dark:ghost":
    "dark:text-gray-300 dark:hover:bg-gray-700 dark:hover:text-white dark:active:scale-95 dark:focus:ring-gray-500",
  "dark:ghost-primary":
    "dark:text-blue-400 dark:hover:bg-blue-700 dark:hover:text-white dark:active:scale-95 dark:focus:ring-blue-500",
  "dark:gradient":
    "dark:bg-gradient-to-r dark:from-blue-600 dark:to-indigo-600 dark:text-white dark:hover:from-blue-700 dark:hover:to-indigo-700 dark:hover:shadow-lg dark:hover:shadow-blue-500/50 dark:active:scale-95 dark:focus:ring-blue-500",
};

const glowClasses = {
  primary: "bg-blue-400",
  success: "bg-green-400",
};

const shineEffect =
  "bg-gradient-to-r from-transparent via-white/10 to-transparent -translate-x-full group-hover:translate-x-full transition-transform duration-1000";

const getIconComponent = (iconName) => {
  const icons = {
    upload: "UploadIcon",
    check: "CheckIcon",
    trash: "TrashIcon",
    download: "DownloadIcon",
    left_arrow: "LeftArrowIcon",
    right_arrow: "RightArrowIcon",
    add: "AddIcon", // Added AddIcon mapping
    edit: "EditIcon", // Added EditIcon mapping
    view: "ViewIcon", // Added ViewIcon mapping
    save: "SaveIcon", // Added SaveIcon mapping
    user_approver: "UserApproverIcon", // Added UserApproverIcon mapping
    close: "CloseIcon", // Added CloseIcon mapping
    chat: "ChatIcon", // Added ChatIcon mapping
  };
  return icons[iconName] || null;
};

const iconComponents = {
  UploadIcon: defineAsyncComponent(() => import("./icon/Upload.vue")),
  CheckIcon: defineAsyncComponent(() => import("./icon/Check.vue")),
  TrashIcon: defineAsyncComponent(() => import("./icon/Trash.vue")),
  DownloadIcon: defineAsyncComponent(() => import("./icon/Download.vue")),
  LeftArrowIcon: defineAsyncComponent(() => import("./icon/LeftArrow.vue")),
  RightArrowIcon: defineAsyncComponent(() => import("./icon/RightArrow.vue")),
  AddIcon: defineAsyncComponent(() => import("./icon/Add.vue")), // Added AddIcon component
  EditIcon: defineAsyncComponent(() => import("./icon/Edit.vue")), // Added EditIcon component
  ViewIcon: defineAsyncComponent(() => import("./icon/View.vue")), // Added ViewIcon component
  SaveIcon: defineAsyncComponent(() => import("./icon/Save.vue")), // Added SaveIcon component
  CloseIcon: defineAsyncComponent(() => import("./icon/Close.vue")),
  ChatIcon: defineAsyncComponent(() => import("./icon/Chat.vue")),
  UserApproverIcon: defineAsyncComponent(
    () => import("./icon/UserApprover.vue"),
  ), // Added UserApproverIcon component
};
</script>

<style scoped>
/* Ensure smooth animations */
button {
  transform-origin: center;
}

/* Prevent text selection during clicks */
button {
  -webkit-user-select: none;
  user-select: none;
}

/* Enhanced focus visible for accessibility */
button:focus-visible {
  outline: 2px solid currentColor;
  outline-offset: 2px;
}
</style>
