<template>
  <div class="p-4">
    <!-- Trash Area -->
    <div
      ref="trashRef"
      class="w-32 h-32 bg-red-200 text-red-800 flex items-center justify-center rounded border-2 border-red-400 transition-colors"
    >
      <span>🗑</span>
    </div>
    <!-- Grid Container -->
    <div
      ref="gridRef"
      class="grid-stack bg-gray-100 min-h-[400px] rounded border-2 border-dashed border-gray-300 relative"
    ></div>

    <!-- Buttons and Trash -->
    <div class="flex justify-between mt-4 items-center">
      <div class="flex gap-2">
        <button
          @click="addCard"
          class="px-4 py-2 bg-green-500 text-white rounded hover:bg-green-600"
        >
          Add Card
        </button>
        <button
          @click="generateCode"
          class="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
        >
          Generate Code
        </button>
        <button
          v-if="generatedCode"
          @click="copyCode"
          class="px-4 py-2 bg-gray-800 text-white rounded hover:bg-gray-900"
        >
          Copy Code
        </button>
      </div>
    </div>
    <!-- Generated Code -->
    <pre
      v-if="generatedCode"
      class="mt-4 bg-gray-200 p-4 rounded overflow-x-auto text-sm whitespace-pre-wrap"
      >{{ generatedCode }}
    </pre>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { GridStack } from "gridstack";
import "gridstack/dist/gridstack.min.css";

const gridRef = ref(null);
const trashRef = ref(null);
let grid = null;
const cardCount = ref(1);
const generatedCode = ref("");
const isOverTrash = ref(false);

// Initialize Gridstack
onMounted(() => {
  grid = GridStack.init(
    {
      cellHeight: 100,
      margin: 10,
      float: true,
      draggable: { containment: true }, // allow dragging outside
      resizable: { handles: "se" }, // resize only bottom-right
    },
    gridRef.value
  );

  addCard(); // optional: start with 1 card

  // Highlight trash area when hovering
  grid.on("drag", (event, el) => {
    const widgetEl = el.closest(".grid-stack-item");
    if (!widgetEl) return;

    const cardRect = widgetEl.getBoundingClientRect();
    const trashRect = trashRef.value.getBoundingClientRect();

    isOverTrash.value =
      cardRect.left < trashRect.right &&
      cardRect.right > trashRect.left &&
      cardRect.top < trashRect.bottom &&
      cardRect.bottom > trashRect.top;

    trashRef.value.classList.toggle("bg-red-400", isOverTrash.value);
  });

  // Delete card when dropped in trash
  grid.on("dragstop", (event, el) => {
    if (!trashRef.value) return;

    const widgetEl = el.closest(".grid-stack-item");
    if (!widgetEl) return;

    /* // Get card position
    const cardRect = widgetEl.getBoundingClientRect();
    const trashRect = trashRef.value.getBoundingClientRect();

    // Collision detection
    const isOverTrash =
      cardRect.left < trashRect.right &&
      cardRect.right > trashRect.left &&
      cardRect.top < trashRect.bottom &&
      cardRect.bottom > trashRect.top; */

    if (isOverTrash.value) {
      grid.removeWidget(widgetEl);
      trashRef.value.classList.remove("bg-red-400");
      isOverTrash.value = false;
    }
  });
});

// Add a new card
function addCard() {
  if (!grid) return;

  const item = document.createElement("div");
  item.classList.add("grid-stack-item");

  const content = document.createElement("div");
  content.classList.add(
    "grid-stack-item-content",
    "bg-blue-200",
    "p-2",
    "rounded",
    "flex",
    "items-center",
    "justify-center"
  );
  content.textContent = `Card ${cardCount.value}`;

  item.appendChild(content);
  grid.addWidget(item, { x: 0, y: 0, w: 2, h: 1 });

  cardCount.value++;
}

// Generate Tailwind HTML code block
function generateCode() {
  if (!grid) return;
  const widgets = grid.engine.nodes;
  let html = "";
  const parentRect = grid.el.getBoundingClientRect();

  widgets.forEach((w) => {
    const content = w.el.querySelector(".grid-stack-item-content").innerHTML;
    const rect = w.el.getBoundingClientRect();
    const top = Math.round(rect.top - parentRect.top);
    const left = Math.round(rect.left - parentRect.left);
    const width = Math.round(rect.width);
    const height = Math.round(rect.height);

    html += `<div class="absolute top-[${top}px] left-[${left}px] w-[${width}px] h-[${height}px] bg-blue-200 p-2 rounded flex items-center justify-center">${content}</div>\n`;
  });

  generatedCode.value = html;
}

// Copy code to clipboard
function copyCode() {
  navigator.clipboard.writeText(generatedCode.value);
  alert("Copied to clipboard!");
}
</script>

<style scoped>
.grid-stack {
  position: relative;
}
.grid-stack-item-content {
  height: 100%;
  width: 100%;
  position: relative;
}
</style>
