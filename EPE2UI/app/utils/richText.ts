const BLOCK_END_TAGS = /<\/\s*(p|div|li|h[1-6]|blockquote|tr|td|th)\s*>/gi;

export const stripHtml = (html?: string | null): string => {
  if (!html) return "";
  const withBreaks = html
    .replace(/<\s*br\s*\/?\s*>/gi, "\n")
    .replace(BLOCK_END_TAGS, "\n");
  const withoutTags = withBreaks.replace(/<[^>]*>/g, "");
  if (typeof document === "undefined") {
    return withoutTags.replace(/\s+/g, " ").trim();
  }
  const holder = document.createElement("textarea");
  holder.innerHTML = withoutTags;
  return (holder.value || withoutTags).replace(/\s+/g, " ").trim();
};
