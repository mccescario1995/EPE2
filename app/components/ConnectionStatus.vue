```vue
<script setup lang="ts">
/**
 * <ConnectionStatus />
 *
 * Toast-style connection notification.
 *
 * Features:
 * - Hidden when connection is good
 * - Shows when connection is slow, poor, or offline
 * - Shows "Connection restored" after recovery
 * - Automatically hides after recovery
 * - Manual dismiss stays dismissed for the current route
 * - Automatically re-arms when navigating to another route
 * - Supports four positions
 * - Supports latency information
 * - Tailwind CSS only
 * - Light + dark mode
 */

interface Props {
  /**
   * Show latency information when connection is slow/poor.
   */
  showLatency?: boolean

  /**
   * Toast position.
   */
  position?:
    | 'top-right'
    | 'top-left'
    | 'bottom-right'
    | 'bottom-left'

  /**
   * How long to keep showing the
   * "Connection restored" message.
   */
  recoveredHoldMs?: number

  /**
   * How often to poll while connection is good.
   */
  baseIntervalMs?: number

  /**
   * How often to poll while connection is bad.
   */
  fastIntervalMs?: number

  /**
   * Endpoint/file to ping.
   */
  target?: string
}

const props = withDefaults(
  defineProps<Props>(),
  {
    showLatency: true,
    position: 'top-right',
    recoveredHoldMs: 2500,
    baseIntervalMs: 45000,
    fastIntervalMs: 8000,
    target: '/api/ping',
  }
)

/**
 * Connection monitoring.
 */
const {
  latency,
  status,
  check,
} = usePing({
  baseIntervalMs: props.baseIntervalMs,
  fastIntervalMs: props.fastIntervalMs,
  target: props.target,
})

const route = useRoute()

/**
 * Status labels.
 */
const labels: Record<string, string> = {
  good: 'Connection restored',
  medium: 'Your connection looks slow',
  bad: 'Your connection looks poor',
  offline: 'You are offline',
  checking: 'Checking connection…',
}

/**
 * Only these statuses trigger the popup.
 */
const isBad = (value: string) => {
  return (
    value === 'medium' ||
    value === 'bad' ||
    value === 'offline'
  )
}

/**
 * Toast visibility.
 */
const visible = ref(false)

/**
 * Whether the toast is showing
 * the recovery message.
 */
const showRecovered = ref(false)

/**
 * Prevents the toast from reappearing
 * after manual dismissal on the same route.
 */
const dismissedForThisPage = ref(false)

/**
 * Recovery timeout.
 */
let recoveredTimer: ReturnType<typeof setTimeout> | null = null

/**
 * Tracks whether the connection was previously bad.
 */
let wasBad = false

/**
 * Re-arm the notification when navigating
 * to another route.
 */
watch(
  () => route.fullPath,
  () => {
    dismissedForThisPage.value = false
  }
)

/**
 * Watch connection status.
 */
watch(
  status,
  (newStatus) => {
    /**
     * Connection is bad.
     */
    if (isBad(newStatus)) {
      if (recoveredTimer) {
        clearTimeout(recoveredTimer)
        recoveredTimer = null
      }

      showRecovered.value = false
      wasBad = true

      if (!dismissedForThisPage.value) {
        visible.value = true
      }

      return
    }

    /**
     * Connection recovered.
     */
    if (
      newStatus === 'good' &&
      wasBad
    ) {
      if (!dismissedForThisPage.value) {
        showRecovered.value = true
        visible.value = true

        recoveredTimer = setTimeout(() => {
          visible.value = false
          showRecovered.value = false
          wasBad = false
          recoveredTimer = null
        }, props.recoveredHoldMs)
      } else {
        wasBad = false
      }

      return
    }

    /**
     * Good without previous failure,
     * or checking.
     */
    if (!wasBad) {
      visible.value = false
    }
  }
)

/**
 * Toast label.
 */
const label = computed(() => {
  if (showRecovered.value) {
    return labels.good
  }

  return labels[status.value] ?? labels.checking
})

/**
 * Toast position.
 */
const positionClasses = computed(() => {
  switch (props.position) {
    case 'top-left':
      return 'top-4 left-4'

    case 'top-right':
      return 'top-4 right-4'

    case 'bottom-left':
      return 'bottom-4 left-4'

    case 'bottom-right':
      return 'bottom-4 right-4'

    default:
      return 'top-4 right-4'
  }
})

/**
 * Status dot.
 */
const dotClasses = computed(() => {
  /**
   * Recovered.
   */
  if (showRecovered.value) {
    return 'bg-green-500'
  }

  /**
   * Slow connection.
   */
  if (status.value === 'medium') {
    return 'bg-amber-500'
  }

  /**
   * Bad / offline.
   */
  if (
    status.value === 'bad' ||
    status.value === 'offline'
  ) {
    return 'bg-red-500'
  }

  /**
   * Good.
   */
  if (status.value === 'good') {
    return 'bg-green-500'
  }

  return 'bg-gray-400 dark:bg-gray-500'
})

/**
 * Slide direction.
 */
const transitionFromClass = computed(() => {
  const isLeft =
    props.position === 'top-left' ||
    props.position === 'bottom-left'

  return isLeft
    ? 'opacity-0 -translate-x-4'
    : 'opacity-0 translate-x-4'
})

/**
 * Dismiss the notification.
 */
const dismiss = () => {
  if (recoveredTimer) {
    clearTimeout(recoveredTimer)
    recoveredTimer = null
  }

  visible.value = false
  dismissedForThisPage.value = true
}
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="
        transition-all
        duration-300
        ease-out
        motion-reduce:transition-opacity
        motion-reduce:duration-150
      "
      leave-active-class="
        transition-all
        duration-300
        ease-in
        motion-reduce:transition-opacity
        motion-reduce:duration-150
      "
      :enter-from-class="transitionFromClass"
      enter-to-class="opacity-100 translate-x-0"
      leave-from-class="opacity-100 translate-x-0"
      :leave-to-class="transitionFromClass"
    >
      <div
        v-if="visible"
        :class="[
          /**
           * Position
           */
          'fixed z-[9999]',
          positionClasses,

          /**
           * Layout
           */
          'flex items-start gap-2',

          /**
           * Width
           */
          'w-[320px]',
          'max-w-[calc(100vw-2rem)]',

          /**
           * Padding
           */
          'px-3 py-2.5',

          /**
           * Border radius
           */
          'rounded-[10px]',

          /**
           * ==================================
           * LIGHT MODE
           * ==================================
           */

          /**
           * Background
           */
          'bg-white',

          /**
           * Text
           */
          'text-gray-900',

          /**
           * Border
           */
          'border border-gray-900',

          /**
           * Shadow
           */
          'shadow-lg shadow-gray-900/10',

          /**
           * ==================================
           * DARK MODE
           * ==================================
           */

          'dark:bg-gray-800',
          'dark:text-gray-100',
          'dark:border-gray-700',
          'dark:shadow-black/30',

          /**
           * Typography
           */
          'font-sans',
          'text-[0.83rem]',
          'leading-[1.35]',
        ]"
        role="status"
        aria-live="polite"
      >
        <!--
          Status indicator
        -->
        <span
          :class="[
            'mt-1.5',
            'h-[9px] w-[9px]',
            'shrink-0',
            'rounded-full',
            dotClasses,
          ]"
          aria-hidden="true"
        />

        <!--
          Notification body
        -->
        <div
          class="
            flex
            min-w-0
            flex-1
            flex-col
            gap-0.5
          "
        >
          <!--
            Status label
          -->
          <span
            class="
              font-semibold
              text-gray-900
              dark:text-gray-100
            "
          >
            {{ label }}
          </span>

          <!--
            Latency information
          -->
          <span
            v-if="
              showLatency &&
              latency !== null &&
              !showRecovered
            "
            class="
              text-[0.76rem]
              text-gray-500
              dark:text-gray-400
            "
          >
            {{ latency }}ms — The request is taking
            longer than expected due to a possible
            network issue.
          </span>
        </div>

        <!--
          Retry button
        -->
        <button
          type="button"
          title="Check again"
          aria-label="Check connection again"
          class="
            shrink-0
            cursor-pointer
            border-0
            bg-transparent
            p-0.5
            text-[0.85rem]
            leading-none

            text-gray-500
            hover:text-gray-900

            transition-colors

            focus:outline-none
            focus-visible:ring-2
            focus-visible:ring-gray-400
            focus-visible:ring-offset-1
            focus-visible:ring-offset-white

            dark:text-gray-400
            dark:hover:text-white
            dark:focus-visible:ring-gray-500
            dark:focus-visible:ring-offset-gray-800
          "
          @click="check"
        >
          ↻
        </button>

        <!--
          Close button
        -->
        <button
          type="button"
          title="Dismiss"
          aria-label="Dismiss connection notification"
          class="
            shrink-0
            cursor-pointer
            border-0
            bg-transparent
            p-0.5
            text-[0.85rem]
            leading-none

            text-gray-500
            hover:text-gray-900

            transition-colors

            focus:outline-none
            focus-visible:ring-2
            focus-visible:ring-gray-400
            focus-visible:ring-offset-1
            focus-visible:ring-offset-white

            dark:text-gray-400
            dark:hover:text-white
            dark:focus-visible:ring-gray-500
            dark:focus-visible:ring-offset-gray-800
          "
          @click="dismiss"
        >
          ✕
        </button>
      </div>
    </Transition>
  </Teleport>
</template>
```
