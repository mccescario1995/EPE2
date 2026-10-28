// composables/usePing.ts
// Measures round-trip latency to a target URL. Tuned to be light on both
// server load and user data:
//   - polls slowly while the connection is fine, faster only while
//     confirming a bad state or watching for recovery
//   - takes just 1 sample for routine checks, and only takes extra
//     confirmation samples when a status change looks likely (avoids
//     both false alarms from single blips AND wasted requests)
//   - pauses entirely when the tab is hidden/backgrounded

export type PingStatus = 'good' | 'medium' | 'bad' | 'offline' | 'checking'

interface UsePingOptions {
  /** ms threshold under which we call it "good" */
  goodThreshold?: number
  /** ms threshold under which we call it "medium" (above = "bad") */
  mediumThreshold?: number
  /** per-request timeout in ms */
  timeout?: number
  /** how often to poll while status is "good" — can be slow, nothing's wrong */
  baseIntervalMs?: number
  /** how often to poll while status is bad/medium/offline — faster, to catch recovery sooner */
  fastIntervalMs?: number
  /** samples taken on a routine check (cheap, just 1 request) */
  routineSamples?: number
  /** samples taken to CONFIRM a suspected status change (avoids one-off blips causing false toasts) */
  confirmSamples?: number
  /**
   * What to ping. Defaults to '/api/ping' — a server route, which requires
   * a Node/serverless server running behind the app.
   *
   * If you deploy with `nuxt generate` + upload static files (no server
   * process, e.g. shared/cPanel-style hosting), that route will 404 in
   * production even though it works on localhost. Point this at a plain
   * static file instead, e.g. '/ping.txt' (or '/lrc/ping.txt' if your
   * site is served from a subpath) — it still measures a real HTTP
   * round trip, no server required.
   */
  target?: string
}

export const usePing = (options: UsePingOptions = {}) => {
  const {
    goodThreshold = 150,
    mediumThreshold = 400,
    timeout = 5000,
    baseIntervalMs = 45000,
    fastIntervalMs = 8000,
    routineSamples = 1,
    confirmSamples = 3,
    target = '/api/ping',
  } = options

  const latency = ref<number | null>(null)
  const status = ref<PingStatus>('checking')
  const lastCheckedAt = ref<Date | null>(null)

  let timerHandle: ReturnType<typeof setTimeout> | null = null
  let stopped = false

  const singlePing = async (): Promise<number | null> => {
    const start = performance.now()
    try {
      // cache-bust so we don't measure a cached/served-from-disk response
      await $fetch(`${target}?_=${Date.now()}`, { timeout })
      return Math.round(performance.now() - start)
    } catch {
      return null
    }
  }

  const median = (nums: number[]) => {
    const sorted = [...nums].sort((a, b) => a - b)
    const mid = Math.floor(sorted.length / 2)
    return sorted.length % 2 !== 0
      ? sorted[mid]
      : Math.round((sorted[mid - 1] + sorted[mid]) / 2)
  }

  const deriveStatus = (ms: number): PingStatus => {
    if (ms < goodThreshold) return 'good'
    if (ms < mediumThreshold) return 'medium'
    return 'bad'
  }

  const sampleN = async (n: number): Promise<number | null> => {
    const results: number[] = []
    for (let i = 0; i < n; i++) {
      const ms = await singlePing()
      if (ms !== null) results.push(ms)
    }
    return results.length ? median(results) : null
  }

  const check = async () => {
    if (typeof navigator !== 'undefined' && !navigator.onLine) {
      status.value = 'offline'
      latency.value = null
      lastCheckedAt.value = new Date()
      scheduleNext()
      return
    }

    // routine check: just 1 request most of the time
    const routineMs = await sampleN(routineSamples)
    lastCheckedAt.value = new Date()

    if (routineMs === null) {
      status.value = 'offline'
      latency.value = null
      scheduleNext()
      return
    }

    const tentative = deriveStatus(routineMs)

    // if the tentative status differs from what we currently report,
    // confirm with a couple more samples before actually flipping —
    // this avoids one lucky/unlucky ping causing a false toast.
    if (tentative !== status.value) {
      const confirmMs = await sampleN(confirmSamples)
      lastCheckedAt.value = new Date()
      if (confirmMs === null) {
        status.value = 'offline'
        latency.value = null
      } else {
        latency.value = confirmMs
        status.value = deriveStatus(confirmMs)
      }
    } else {
      latency.value = routineMs
      status.value = tentative
    }

    scheduleNext()
  }

  const scheduleNext = () => {
    if (stopped) return
    if (timerHandle) clearTimeout(timerHandle)
    // poll faster while something's wrong, so recovery is caught quickly;
    // poll slowly while things are fine, to save requests/data.
    const next = status.value === 'good' ? baseIntervalMs : fastIntervalMs
    timerHandle = setTimeout(check, next)
  }

  const startPolling = () => {
    stopped = false
    check()
  }

  const stopPolling = () => {
    stopped = true
    if (timerHandle) {
      clearTimeout(timerHandle)
      timerHandle = null
    }
  }

  const handleVisibilityChange = () => {
    if (typeof document === 'undefined') return
    if (document.hidden) {
      // tab backgrounded — stop pinging entirely, no point spending
      // requests/data on a check the user can't see
      stopPolling()
    } else {
      // tab back in focus — check right away, then resume normal polling
      startPolling()
    }
  }

  onMounted(() => {
    if (typeof document === 'undefined' || !document.hidden) {
      startPolling()
    }
    window.addEventListener('online', check)
    window.addEventListener('offline', check)
    document.addEventListener('visibilitychange', handleVisibilityChange)
  })

  onUnmounted(() => {
    stopPolling()
    window.removeEventListener('online', check)
    window.removeEventListener('offline', check)
    document.removeEventListener('visibilitychange', handleVisibilityChange)
  })

  return {
    latency,
    status,
    lastCheckedAt,
    check,
    startPolling,
    stopPolling,
  }
}