<template>
  <!--
    CalendarPicker.vue
    - Fully customizable Date Picker/Calendar for Nuxt 3 + Tailwind CSS
    - Supports single or range selection, min/max, block specific dates, disable past
    - Smart popover that auto-flips (top/bottom & left/right) based on viewport overflow

    USAGE (example):
      <CalendarPicker
        v-model="selected"
        :mode="'range'"
        :min-date="new Date(2024,0,1)"
        :max-date="new Date(2026,11,31)"
        :blocked-dates="[new Date(2025,8,21)]"
        :disable-past="true"
        color="blue"
        size="md"
      />
  -->
  <div class="relative inline-block" ref="rootEl">
    <!-- Trigger (default) -->
    <button
      ref="triggerEl"
      type="button"
      class="inline-flex items-center gap-2 rounded-2xl border px-3 py-2 shadow-sm hover:shadow transition focus:outline-none focus:ring-2 focus:ring-offset-2"
      :class="[
        `border-gray-300 bg-white focus:ring-${color}-500`,
        sizeClasses.trigger,
        textSizeClass,
      ]"
      @click="toggle()"
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="currentColor"
        class="w-5 h-5 opacity-70"
      >
        <path
          d="M7 2a1 1 0 0 1 1 1v1h8V3a1 1 0 1 1 2 0v1h1a3 3 0 0 1 3 3v11a3 3 0 0 1-3 3H5a3 3 0 0 1-3-3V7a3 3 0 0 1 3-3h1V3a1 1 0 1 1 2 0v1Zm13 7H4v9a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1V9Z"
        />
      </svg>
      <span class="truncate max-w-[16rem]">
        {{ displayValue }}
      </span>
    </button>

    <!-- Popover -->
    <teleport to="body">
      <div v-if="open" ref="panelEl" class="fixed z-1000" :style="panelStyle">
        <div class="fixed inset-0" @click="close()"></div>

        <div
          class="relative mx-2 rounded-2xl border bg-white shadow-xl ring-1 ring-black/5"
          :class="sizeClasses.panel"
        >
          <div
            class="absolute w-3 h-3 bg-white border border-gray-200 rotate-45"
            :class="arrowClass"
          ></div>

          <!-- Header -->
          <div class="flex items-center justify-between p-3 border-b">
            <button
              type="button"
              class="p-2 rounded-xl hover:bg-gray-100"
              @click="prevMonth()"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
                class="w-5 h-5"
              >
                <path
                  fill-rule="evenodd"
                  d="M15.78 4.22a.75.75 0 0 1 0 1.06L9.06 12l6.72 6.72a.75.75 0 1 1-1.06 1.06l-7.25-7.25a.75.75 0 0 1 0-1.06l7.25-7.25a.75.75 0 0 1 1.06 0Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
            <div class="font-medium" :class="textSizeClass">
              {{ monthLabel }} {{ visibleYear }}
            </div>
            <button
              type="button"
              class="p-2 rounded-xl hover:bg-gray-100"
              @click="nextMonth()"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="currentColor"
                viewBox="0 0 24 24"
                class="w-5 h-5"
              >
                <path
                  fill-rule="evenodd"
                  d="M8.22 19.78a.75.75 0 0 1 0-1.06L14.94 12 8.22 5.28a.75.75 0 1 1 1.06-1.06l7.25 7.25a.75.75 0 0 1 0 1.06l-7.25 7.25a.75.75 0 0 1-1.06 0Z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
          </div>

          <!-- Weekdays -->
          <div class="grid grid-cols-7 gap-y-1 px-3 pt-2">
            <div
              v-for="d in 7"
              :key="'wd' + d"
              class="text-center text-xs uppercase tracking-wide text-gray-500"
            >
              {{ weekdayLabel(d - 1) }}
            </div>
          </div>

          <!-- Days -->
          <div class="grid grid-cols-7 gap-1 p-3">
            <button
              v-for="cell in monthCells"
              :key="cell.key"
              type="button"
              class="relative aspect-square rounded-xl border text-center select-none focus:outline-none focus:ring-2"
              :class="dayButtonClass(cell)"
              :disabled="cell.disabled"
              @click="onDayClick(cell.date)"
              @mouseenter="onHover(cell.date)"
            >
              <!-- Range background highlight -->
              <span
                v-if="inRange(cell.date)"
                class="absolute inset-0 rounded-md"
                :class="`bg-${rangeColor}-200`"
              ></span>

              <!-- Day number -->
              <span class="relative z-10">{{ cell.date.getDate() }}</span>
            </button>
          </div>

          <!-- Footer -->
          <div class="flex items-center justify-between gap-2 p-3 border-t">
            <div
              class="text-xs text-gray-500"
              :class="{ 'text-red-600': hasError }"
            >
              {{ helperText }}
            </div>
            <div class="flex items-center gap-2">
              <button
                type="button"
                class="px-3 py-1.5 rounded-xl border hover:bg-gray-50"
                @click="clearSelection"
              >
                Clear
              </button>
              <button
                type="button"
                class="px-3 py-1.5 rounded-xl text-white shadow"
                :class="`bg-${color}-600 hover:bg-${color}-700`"
                @click="apply"
              >
                Apply
              </button>
            </div>
          </div>
        </div>
      </div>
    </teleport>
  </div>
</template>

<script setup lang="ts">
/*****************************************************
 * CalendarPicker.vue (Nuxt 3 + TailwindCSS)
 * ---------------------------------------------------
 * Focus: correctness, edge cases, performance, DX.
 * - No external deps; all logic self-contained.
 * - Emits v-model updates and close/apply events.
 * - Smart popover positioning with viewport flip.
 *****************************************************/
import {
  computed,
  onBeforeUnmount,
  onMounted,
  reactive,
  ref,
  watch,
} from "vue";

/* ------------------------ Props & Emits ------------------------ */
interface RangeValue {
  start: Date | null;
  end: Date | null;
}
type Model = Date | RangeValue | null;
type Mode = "single" | "range";

defineOptions({ name: "CalendarPicker" });

const props = withDefaults(
  defineProps<{
    modelValue: Model;
    mode?: Mode;
    minDate?: Date | null;
    maxDate?: Date | null;
    blockedDates?: (Date | string)[];
    disablePast?: boolean;
    allowSameDayRange?: boolean;
    color?: string;
    rangeColor?: string; // NEW prop for range highlight color
    size?: "sm" | "md" | "lg";
    textSize?: "xs" | "sm" | "base";
    firstDayOfWeek?: 0 | 1 | 2 | 3 | 4 | 5 | 6;
    initialMonth?: Date;
  }>(),
  {
    mode: "single",
    blockedDates: () => [],
    disablePast: false,
    allowSameDayRange: true,
    color: "gray",
    rangeColor: "blue", // default same as color
    size: "md",
    textSize: "base",
    firstDayOfWeek: 0,
  }
);

const emit = defineEmits(["update:modelValue", "apply", "open", "close"]);

/* ------------------------ State ------------------------ */
const open = ref(false);
const rootEl = ref<HTMLElement | null>(null);
const triggerEl = ref<HTMLElement | null>(null);
const panelEl = ref<HTMLElement | null>(null);

// Visible month/year state
const today = new Date();
const visible = reactive({
  // Start from initialMonth if valid, else from modelValue, else today
  month: (
    props.initialMonth ??
    (props.mode === "single" && props.modelValue instanceof Date
      ? props.modelValue
      : today)
  ).getMonth(),
  year: (
    props.initialMonth ??
    (props.mode === "single" && props.modelValue instanceof Date
      ? props.modelValue
      : today)
  ).getFullYear(),
});

// Hover date for range preview (no heavy reactivity)
const hoverDate = ref<Date | null>(null);

/* ------------------------ Derived + Theming ------------------------ */
const textSizeClass = computed(
  () => ({ xs: "text-xs", sm: "text-sm", base: "text-base" }[props.textSize])
);

const sizeClasses = computed(() => {
  // Change paddings and widths by size for better density control
  const map = {
    sm: { trigger: "px-2 py-1 text-sm", panel: "w-[18rem]" },
    md: { trigger: "px-3 py-2 text-base", panel: "w-[20rem]" },
    lg: { trigger: "px-4 py-3 text-lg", panel: "w-[22rem]" },
  } as const;
  return map[props.size];
});

const monthLabel = computed(() =>
  new Intl.DateTimeFormat(undefined, { month: "long" }).format(
    new Date(visible.year, visible.month, 1)
  )
);
const visibleYear = computed(() => visible.year);

/* ------------------------ Utilities ------------------------ */
function normalizeDate(d: Date) {
  // Normalize to remove time => compare consistently
  const nd = new Date(d);
  nd.setHours(0, 0, 0, 0);
  return nd;
}

function isSameDay(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

function isBefore(a: Date, b: Date) {
  return normalizeDate(a).getTime() < normalizeDate(b).getTime();
}
function isAfter(a: Date, b: Date) {
  return normalizeDate(a).getTime() > normalizeDate(b).getTime();
}

function clampDate(d: Date, min?: Date | null, max?: Date | null) {
  let x = normalizeDate(d);
  if (min && isBefore(x, min)) x = normalizeDate(min);
  if (max && isAfter(x, max)) x = normalizeDate(max);
  return x;
}

function toDate(v: Date | string) {
  return typeof v === "string" ? new Date(v) : v;
}

function inBlocked(date: Date) {
  const nd = normalizeDate(date).getTime();
  return (props.blockedDates ?? []).some(
    (x) => normalizeDate(toDate(x)).getTime() === nd
  );
}

function isDisabled(date: Date) {
  const nd = normalizeDate(date);
  // Rule: block past if requested
  if (props.disablePast && isBefore(nd, today)) return true;
  // Rule: block out-of-range
  if (props.minDate && isBefore(nd, props.minDate)) return true;
  if (props.maxDate && isAfter(nd, props.maxDate)) return true;
  // Rule: explicit blockedDates
  if (inBlocked(nd)) return true;
  return false;
}

/* ------------------------ Month Cells (fast) ------------------------ */
const monthCells = computed(() => {
  // Compute leading offset given firstDayOfWeek
  const firstOfMonth = new Date(visible.year, visible.month, 1);
  const startWeekday = (firstOfMonth.getDay() - props.firstDayOfWeek + 7) % 7;
  const daysInMonth = new Date(visible.year, visible.month + 1, 0).getDate();

  const cells: Array<{
    key: string;
    date: Date;
    disabled: boolean;
    outside: boolean;
  }> = [];

  // Leading days (from previous month) for consistent 6 rows grid feel
  for (let i = 0; i < startWeekday; i++) {
    const d = new Date(visible.year, visible.month, 1 - (startWeekday - i));
    cells.push({
      key: `p-${d.toDateString()}`,
      date: d,
      disabled: true,
      outside: true,
    });
  }

  // Current month days
  for (let day = 1; day <= daysInMonth; day++) {
    const d = new Date(visible.year, visible.month, day);
    cells.push({
      key: `c-${d.toDateString()}`,
      date: d,
      disabled: isDisabled(d),
      outside: false,
    });
  }

  // Trailing days to complete week rows (up to 42 cells max)
  while (cells.length % 7 !== 0) {
    const last =
      cells[cells.length - 1]?.date ??
      new Date(visible.year, visible.month, daysInMonth);
    const d = new Date(last);
    d.setDate(d.getDate() + 1);
    cells.push({
      key: `n-${d.toDateString()}`,
      date: d,
      disabled: true,
      outside: true,
    });
  }

  return cells;
});

/* ------------------------ Selection state ------------------------ */
const internal = ref<Model>(
  props.modelValue ??
    (props.mode === "range" ? { start: null, end: null } : null)
);

watch(
  () => props.modelValue,
  (nv) => {
    internal.value = nv;
  }
);

function coerceIntoRange(v: Model): RangeValue {
  // Safely map Model into a RangeValue structure
  if (!v || v instanceof Date) return { start: null, end: null };
  const st = v.start ? normalizeDate(v.start) : null;
  const en = v.end ? normalizeDate(v.end) : null;
  return { start: st, end: en };
}

const hasError = ref(false);
const helperText = computed(() => {
  if (hasError.value)
    return "Invalid selection: respects min/max, blocks, and ordering.";
  if (props.mode === "range") return "Pick a start date, then an end date.";
  return "Pick a date.";
});

function onDayClick(date: Date) {
  if (isDisabled(date)) return;

  if (props.mode === "single") {
    const d = clampDate(date, props.minDate, props.maxDate);
    if (inBlocked(d)) {
      hasError.value = true;
      return;
    }
    internal.value = d;
    // Keep panel open until Apply is pressed; change behavior if you prefer instant close
    return;
  }

  // Range mode
  const curr = coerceIntoRange(internal.value);
  if (!curr.start || (curr.start && curr.end)) {
    // Start a new range
    internal.value = { start: date, end: null };
  } else {
    // Completing the range
    let start = curr.start;
    let end = date;
    // Auto-order
    if (isBefore(end, start)) {
      const tmp = start;
      start = end;
      end = tmp;
    }
    // Same-day range allowed?
    if (!props.allowSameDayRange && isSameDay(start, end)) {
      hasError.value = true;
      return;
    }
    // Validate against min/max/blocked
    const days: Date[] = [start, end];
    for (const d of days) {
      if (isDisabled(d)) {
        hasError.value = true;
        return;
      }
    }
    // Note: If you need to ensure no blocked dates inside the span, add a scan here (skipped for perf)
    internal.value = { start, end };
  }
}

function onHover(date: Date) {
  hoverDate.value = date;
}

function clearSelection() {
  internal.value = props.mode === "range" ? { start: null, end: null } : null;
  hasError.value = false;
}

function apply() {
  // Final validation before committing to parent
  hasError.value = false;

  if (props.mode === "single") {
    const d = internal.value as Date | null;
    if (d && isDisabled(d)) {
      hasError.value = true;
      return;
    }
    emit("update:modelValue", d ?? null);
    emit("apply", d ?? null);
    close();
    return;
  }

  const r = coerceIntoRange(internal.value);
  if (r.start && r.end) {
    if (isDisabled(r.start) || isDisabled(r.end)) {
      hasError.value = true;
      return;
    }
    emit("update:modelValue", { start: r.start, end: r.end });
    emit("apply", { start: r.start, end: r.end });
    close();
  } else {
    // Incomplete range - surface a gentle error
    hasError.value = true;
  }
}

/* ------------------------ Display helpers ------------------------ */
const displayValue = computed(() => {
  if (!internal.value) return "Select date";
  if (props.mode === "single") {
    if (internal.value instanceof Date) return formatDate(internal.value);
    return "Select date";
  }
  const r = coerceIntoRange(internal.value);
  const a = r.start ? formatDate(r.start) : "Start";
  const b = r.end ? formatDate(r.end) : "End";
  return `${a} — ${b}`;
});

function formatDate(d: Date) {
  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "short",
    day: "2-digit",
  }).format(d);
}

/* ------------------------ Day Button Classes ------------------------ */
function inCurrentSelection(date: Date) {
  if (props.mode === "single") {
    return internal.value instanceof Date && isSameDay(internal.value, date);
  }
  const r = coerceIntoRange(internal.value);
  if (r.start && r.end) {
    const t = normalizeDate(date).getTime();
    return (
      t >= normalizeDate(r.start).getTime() &&
      t <= normalizeDate(r.end).getTime()
    );
  }
  if (r.start && hoverDate.value && !r.end) {
    // Preview range on hover for better UX
    const a = normalizeDate(r.start).getTime();
    const b = normalizeDate(hoverDate.value).getTime();
    const min = Math.min(a, b),
      max = Math.max(a, b);
    const t = normalizeDate(date).getTime();
    return t >= min && t <= max;
  }
  return false;
}

function isStart(date: Date) {
  if (props.mode !== "range") return false;
  const r = coerceIntoRange(internal.value);
  return !!(r.start && isSameDay(r.start, date));
}

function isEnd(date: Date) {
  if (props.mode !== "range") return false;
  const r = coerceIntoRange(internal.value);
  return !!(r.end && isSameDay(r.end, date));
}

/* function dayButtonClass(cell: {
  date: Date;
  disabled: boolean;
  outside: boolean;
}) {
  const classes: string[] = [];
  // Base
  classes.push("border-transparent");
  if (cell.outside) classes.push("opacity-40");
  if (cell.disabled) classes.push("cursor-not-allowed opacity-40");
  // Selected / in-range
  if (inCurrentSelection(cell.date)) {
    classes.push(`bg-${props.color}-100`);
    classes.push(`ring-${props.color}-500`);
  } else {
    classes.push("hover:bg-gray-100");
  }
  if (isStart(cell.date) || isEnd(cell.date)) {
    classes.push(`!bg-${props.color}-600 !text-white !ring-${props.color}-600`);
  }
  return classes;
} */

/* ------------------------ Navigation ------------------------ */
function prevMonth() {
  const d = new Date(visible.year, visible.month - 1, 1);
  visible.month = d.getMonth();
  visible.year = d.getFullYear();
}

function nextMonth() {
  const d = new Date(visible.year, visible.month + 1, 1);
  visible.month = d.getMonth();
  visible.year = d.getFullYear();
}

function weekdayLabel(index0to6: number) {
  // Shifted by firstDayOfWeek
  const day = (index0to6 + (props.firstDayOfWeek ?? 0)) % 7;
  return new Intl.DateTimeFormat(undefined, { weekday: "short" }).format(
    new Date(2023, 0, 1 + day)
  );
}

/* ------------------------ Popover Positioning (flip smart) ------------------------ */
const panelStyle = ref({});
const placement = ref<"top" | "bottom">("bottom");
const hAlign = ref<"left" | "right">("left");

function computePosition() {
  const trigger = triggerEl.value;
  const panel = panelEl.value;
  if (!trigger || !panel) return;

  const tRect = trigger.getBoundingClientRect();
  const pRect = panel.getBoundingClientRect();
  const vw = window.innerWidth,
    vh = window.innerHeight;

  // Decide vertical placement
  const spaceBelow = vh - tRect.bottom;
  const spaceAbove = tRect.top;
  placement.value =
    spaceBelow < pRect.height + 12 && spaceAbove > spaceBelow
      ? "top"
      : "bottom";

  // Decide horizontal alignment
  const preferredLeft = tRect.left;
  const preferredRight = vw - tRect.right;
  hAlign.value =
    preferredRight < pRect.width && preferredLeft > preferredRight
      ? "right"
      : "left";

  // Compute top/left
  let top =
    placement.value === "bottom"
      ? tRect.bottom + 8
      : tRect.top - pRect.height - 8;
  let left =
    hAlign.value === "left"
      ? Math.max(8, Math.min(preferredLeft, vw - pRect.width - 8))
      : Math.max(8, Math.min(tRect.right - pRect.width, vw - pRect.width - 8));

  panelStyle.value = { top: `${top}px`, left: `${left}px` };
}

const arrowClass = computed(() => {
  // Position the little arrow visually depending on placement & alignment
  const base = "border-gray-200";
  if (placement.value === "bottom") {
    return `${base} -top-1 left-8`;
  } else {
    return `${base} -bottom-1 left-8`;
  }
});

function openPanel() {
  open.value = true;
  emit("open");
  // Wait next frame so panelEl has layout, then compute position
  requestAnimationFrame(() => computePosition());
  window.addEventListener("resize", computePosition, { passive: true });
  window.addEventListener("scroll", computePosition, { passive: true });
  document.addEventListener("keydown", onKeydown);
}

function close() {
  open.value = false;
  emit("close");
  window.removeEventListener("resize", computePosition);
  window.removeEventListener("scroll", computePosition);
  document.removeEventListener("keydown", onKeydown);
}

function toggle() {
  open.value ? close() : openPanel();
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === "Escape") close();
}

// Click-outside handling without external deps
function handleDocClick(e: MouseEvent) {
  const r = rootEl.value;
  const p = panelEl.value;
  if (!r || !p) return;
  const target = e.target as Node;
  if (r.contains(target) || p.contains(target)) return;
  close();
}

onMounted(() => {
  document.addEventListener("click", handleDocClick);
});

onBeforeUnmount(() => {
  document.removeEventListener("click", handleDocClick);
  window.removeEventListener("resize", computePosition);
  window.removeEventListener("scroll", computePosition);
  document.removeEventListener("keydown", onKeydown);
});
/* --- RANGE Highlight --- */
function inRange(date: Date) {
  if (props.mode !== "range") return false;
  const r = coerceIntoRange(internal.value);
  if (r.start && r.end) {
    const t = normalizeDate(date).getTime();
    return (
      t > normalizeDate(r.start).getTime() && t < normalizeDate(r.end).getTime()
    );
  }
  if (r.start && hoverDate.value && !r.end) {
    const a = normalizeDate(r.start).getTime();
    const b = normalizeDate(hoverDate.value).getTime();
    const min = Math.min(a, b),
      max = Math.max(a, b);
    const t = normalizeDate(date).getTime();
    return t > min && t < max;
  }
  return false;
}

/* --- Updated dayButtonClass to support selected dates background --- */
function dayButtonClass(cell: {
  date: Date;
  disabled: boolean;
  outside: boolean;
}) {
  const classes: string[] = ["border"];
  if (cell.outside) classes.push("opacity-40");
  if (cell.disabled) classes.push("cursor-not-allowed opacity-40");

  if (inCurrentSelection(cell.date)) {
    classes.push(`bg-${props.color}-500 text-white`); // selected dates background
  } else {
    classes.push("hover:bg-gray-100");
  }
  return classes;
}
</script>

<style scoped>
/* No fixed colors here beyond borders; component uses Tailwind utility classes
   derived from the `color` prop for primary accents. */
</style>
