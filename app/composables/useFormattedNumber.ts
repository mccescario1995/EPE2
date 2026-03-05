// composables/useFormattedNumber.ts
import { computed } from "vue";

export function useFormattedNumber(refNumber: any) {
  return computed({
    get() {
      const num = parseFloat(refNumber.value);
      if (isNaN(num)) return "";
      return num.toLocaleString("en-US", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      });
    },
    set(val: string) {
      // Allow empty string for delete/backspace
      if (val === "") {
        refNumber.value = 0;
        return;
      }

      // Strip everything except digits and dot
      const clean = val.replace(/[^0-9.]/g, "");

      // Ensure only one dot (.)
      const safe = clean.replace(/^([^.]*)\.?/, (_, intPart) => intPart + ".");

      const parsed = parseFloat(safe);
      refNumber.value = isNaN(parsed) ? 0 : parsed;
    },
  });
}
