<!-- <script setup lang="ts">
/**
 * <ConnectionStatus />
 *
 * Toast-style notification that stays hidden while the connection is
 * fine, and slides in automatically when latency gets bad or the user
 * goes offline — like a normal OS/system network notification. It slides
 * back out on its own once the connection recovers.
 *
 * If the user clicks the ✕ to dismiss it, it stays quiet for the rest of
 * that page — even if the connection is still bad — and only re-arms
 * itself once they navigate to a different route.
 *
 * Usage:
 *   <ConnectionStatus />
 *   <ConnectionStatus position="bottom-left" :recovered-hold-ms="2000" />
 *
 *   <!-- if deployed as a static site (nuxt generate + upload, no
 *        Node server), point at a static file instead of the API route: -->
 <!-- *   <ConnectionStatus target="/lrc/ping.txt" /> -->
 */

interface Props {
  showLatency?: boolean
  position?: 'top-right' | 'top-left' | 'bottom-right' | 'bottom-left'
  /** how long to keep showing the "back to normal" message before hiding, in ms */
  recoveredHoldMs?: number
  /** how often to poll while connection is good (slow — nothing's wrong) */
  baseIntervalMs?: number
  /** how often to poll while connection is bad/medium/offline (faster — watching for recovery) */
  fastIntervalMs?: number
  /**
   * What to ping. Defaults to '/api/ping' (needs a Node/serverless server).
   * If deployed as a static site (nuxt generate + upload, no server),
   * set this to a static file instead, e.g. '/ping.txt' or '/lrc/ping.txt'
   * if served from a subpath.
   */
  target?: string
}

const props = withDefaults(defineProps<Props>(), {
  showLatency: true,
  position: 'top-right',
  recoveredHoldMs: 2500,
  baseIntervalMs: 45000,
  fastIntervalMs: 8000,
  target: '/api/ping',
})

const { latency, status, check } = usePing({
  baseIntervalMs: props.baseIntervalMs,
  fastIntervalMs: props.fastIntervalMs,
  target: props.target,
})

const route = useRoute()

const labels: Record<string, string> = {
  good: 'Connection restored',
  medium: 'Your connection looks slow',
  bad: 'Your connection looks poor',
  offline: 'You are offline',
  checking: 'Checking connection…',
}

// Only these statuses should ever trigger the popup.
const isBad = (s: string) => s === 'medium' || s === 'bad' || s === 'offline'

const visible = ref(false)
const showRecovered = ref(false)
// once the user closes the toast manually, stay quiet — even if status
// stays bad — until they navigate to a different page.
const dismissedForThisPage = ref(false)
let recoveredTimer: ReturnType<typeof setTimeout> | null = null
let wasBad = false

// re-arm the toast whenever the route changes.
watch(
  () => route.fullPath,
  () => {
    dismissedForThisPage.value = false
  }
)

watch(status, (newStatus) => {
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

  // status just recovered to "good" after having been bad — show a brief
  // confirmation, then auto-hide, just like a system reconnect toast.
  // Skip the "restored" message if the user already dismissed it.
  if (newStatus === 'good' && wasBad) {
    if (!dismissedForThisPage.value) {
      showRecovered.value = true
      visible.value = true
      recoveredTimer = setTimeout(() => {
        visible.value = false
        showRecovered.value = false
        wasBad = false
      }, props.recoveredHoldMs)
    } else {
      wasBad = false
    }
    return
  }

  // "good" with nothing previously wrong, or "checking" — stay hidden.
  if (!wasBad) {
    visible.value = false
  }
})

const label = computed(() =>
  showRecovered.value ? labels.good : labels[status.value]
)

const dismiss = () => {
  if (recoveredTimer) clearTimeout(recoveredTimer)
  visible.value = false
  dismissedForThisPage.value = true
}
</script>

<template>
  <Teleport to="body">
    <Transition name="toast">
      <div
        v-if="visible"
        class="conn-toast"
        :class="[`conn-toast--${position}`, showRecovered ? 'conn-toast--good' : `conn-toast--${status}`]"
        role="status"
        aria-live="polite"
      >
        <span class="conn-toast__dot" aria-hidden="true" />
        <div class="conn-toast__body">
          <span class="conn-toast__label">{{ label }}</span>
          <span
            v-if="showLatency && latency !== null && !showRecovered"
            class="conn-toast__latency"
          >{{ latency }}ms — The request is taking longer than expected due to a possible network issue.</span>
        </div>
        <button
          class="conn-toast__retry"
          type="button"
          title="Check again"
          @click="check"
        >
          ↻
        </button>
        <button
          class="conn-toast__close"
          type="button"
          title="Dismiss"
          @click="dismiss"
        >
          ✕
        </button>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.conn-toast {
  position: fixed;
  z-index: 9999;
  display: flex;
  align-items: flex-start;
  gap: 0.55rem;
  max-width: 320px;
  padding: 0.7rem 0.75rem;
  border-radius: 10px;
  background: #1f2124;
  color: #f2f2f2;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.25);
  font-family: inherit;
  font-size: 0.83rem;
  line-height: 1.35;
}

.conn-toast--top-right { top: 1rem; right: 1rem; }
.conn-toast--top-left { top: 1rem; left: 1rem; }
.conn-toast--bottom-right { bottom: 1rem; right: 1rem; }
.conn-toast--bottom-left { bottom: 1rem; left: 1rem; }

.conn-toast__dot {
  width: 9px;
  height: 9px;
  margin-top: 0.3rem;
  border-radius: 50%;
  flex-shrink: 0;
}

.conn-toast--medium .conn-toast__dot { background: #f5a623; }
.conn-toast--bad .conn-toast__dot,
.conn-toast--offline .conn-toast__dot { background: #e74c3c; }
.conn-toast--good .conn-toast__dot { background: #2ecc71; }

.conn-toast__body {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1;
  min-width: 0;
}

.conn-toast__label {
  font-weight: 600;
}

.conn-toast__latency {
  color: #b8bcc2;
  font-size: 0.76rem;
}

.conn-toast__retry,
.conn-toast__close {
  background: transparent;
  border: none;
  color: #b8bcc2;
  cursor: pointer;
  font-size: 0.85rem;
  line-height: 1;
  padding: 0.15rem;
  flex-shrink: 0;
}

.conn-toast__retry:hover,
.conn-toast__close:hover {
  color: #fff;
}

/* slide/fade transition, direction-aware based on position */
.toast-enter-active,
.toast-leave-active {
  transition: transform 0.25s ease, opacity 0.25s ease;
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
}
.conn-toast--top-right.toast-enter-from,
.conn-toast--top-right.toast-leave-to,
.conn-toast--bottom-right.toast-enter-from,
.conn-toast--bottom-right.toast-leave-to {
  transform: translateX(16px);
}
.conn-toast--top-left.toast-enter-from,
.conn-toast--top-left.toast-leave-to,
.conn-toast--bottom-left.toast-enter-from,
.conn-toast--bottom-left.toast-leave-to {
  transform: translateX(-16px);
}

@media (prefers-reduced-motion: reduce) {
  .toast-enter-active,
  .toast-leave-active {
    transition: opacity 0.15s ease;
  }
  .conn-toast--top-right.toast-enter-from,
  .conn-toast--top-right.toast-leave-to,
  .conn-toast--bottom-right.toast-enter-from,
  .conn-toast--bottom-right.toast-leave-to,
  .conn-toast--top-left.toast-enter-from,
  .conn-toast--top-left.toast-leave-to,
  .conn-toast--bottom-left.toast-enter-from,
  .conn-toast--bottom-left.toast-leave-to {
    transform: none;
  }
}
</style> -->


