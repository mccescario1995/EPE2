<template>
  <div
    v-if="readonly"
    :class="customClass || 'w-full text-sm sm:text-base text-gray-900 dark:text-gray-100'"
  >
    <p
      v-if="!modelValue"
      class="text-gray-500 dark:text-gray-400"
    >
      {{ placeholder }}
    </p>
    <p
      v-else-if="isPlainText"
      class="whitespace-pre-wrap"
    >
      {{ modelValue }}
    </p>
    <div v-else-if="editor" class="tiptap-content">
      <TiptapEditorContent :editor="editor" />
    </div>
  </div>

  <div v-else :class="customClass || 'w-full'">
    <label
      v-if="label"
      :for="id"
      class="block mb-1 text-sm font-medium text-gray-700 dark:text-gray-300"
    >
      {{ label }}
    </label>

    <div
      :id="id"
      class="overflow-hidden rounded-lg border bg-white shadow-sm transition-colors dark:bg-gray-900"
      :class="[
        disabled
          ? 'border-gray-200 opacity-60 dark:border-gray-700'
          : 'border-gray-300 focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500 dark:border-gray-600',
      ]"
    >
      <div
        v-if="editor"
        role="toolbar"
        aria-label="Text formatting"
        class="flex flex-wrap items-center gap-y-2 border-b border-gray-200 bg-gray-50 p-2 dark:border-gray-700 dark:bg-gray-800/60"
      >
        <div
          v-for="(group, index) in toolbarGroups"
          :key="index"
          class="flex items-center gap-0.5"
          :class="
            index < toolbarGroups.length - 1
              ? 'mr-1 border-r border-gray-200 pr-2 dark:border-gray-700'
              : ''
          "
        >
          <button
            v-for="button in group"
            :key="button.title"
            type="button"
            :title="button.title"
            :aria-label="button.title"
            :aria-pressed="button.isActive(editor)"
            :disabled="disabled || !button.canRun(editor)"
            class="inline-flex h-8 w-8 items-center justify-center rounded-md transition-colors focus:outline-none focus-visible:ring-2 focus-visible:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-40"
            :class="
              button.isActive(editor)
                ? 'bg-blue-600 text-white'
                : button.danger
                  ? 'text-red-600 hover:bg-red-50 hover:text-red-700 dark:text-red-400 dark:hover:bg-red-500/10'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900 dark:text-gray-300 dark:hover:bg-gray-700 dark:hover:text-white'
            "
            @click="button.run(editor)"
          >
            <component :is="button.icon" class="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
      </div>

      <div
        v-if="editor"
        class="relative"
        :style="{ minHeight: `${rowCount * 1.5}rem` }"
      >
        <div
          v-if="placeholder && isEmpty"
          class="pointer-events-none absolute inset-0 px-4 py-3 text-sm text-gray-400 dark:text-gray-500"
        >
          {{ placeholder }}
        </div>
        <TiptapEditorContent :editor="editor" class="tiptap-content" />
      </div>
    </div>

    <div class="mt-1 flex justify-between dark:bg-gray-800">
      <p v-if="displayError" class="text-xs text-red-500 dark:text-red-400">
        {{ displayError }}
      </p>
      <div
        v-if="maxCharCount || minCharCount"
        class="mt-1 text-xs flex justify-end"
        :class="
          isOverLimit ? 'text-red-500 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'
        "
      >
        {{ charCounterText }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Extension } from '@tiptap/core'
import type { Editor } from '@tiptap/core'
import { Plugin, PluginKey } from '@tiptap/pm/state'
import {
  ArrowPathIcon,
  ArrowUturnLeftIcon,
  ArrowUturnRightIcon,
  BoldIcon,
  ChatBubbleBottomCenterTextIcon,
  CodeBracketIcon,
  DocumentTextIcon,
  H1Icon,
  H2Icon,
  H3Icon,
  ItalicIcon,
  ListBulletIcon,
  MinusIcon,
  NoSymbolIcon,
  QueueListIcon,
  TrashIcon,
} from '@heroicons/vue/24/outline'
import type { Component } from 'vue'

interface Props {
  modelValue?: string
  label?: string
  placeholder?: string
  maxCharCount?: number
  minCharCount?: number
  rowCount?: number
  id?: string
  errorMessage?: string
  customClass?: string
  rules?: ((value: string) => true | string)[]
  disabled?: boolean
  readonly?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: '',
  rowCount: 8,
  id: `editor-${Math.random().toString(36).slice(2, 11)}`,
  errorMessage: '',
  customClass: '',
  rules: () => [],
  disabled: false,
  readonly: false,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'error', message: string): void
}>()

interface ToolbarButtonConfig {
  icon: Component
  title: string
  isActive: (editor: Editor) => boolean
  canRun: (editor: Editor) => boolean
  run: (editor: Editor) => void
  danger?: boolean
}

const BLOCK_SEPARATOR = '\n\n'

const docText = (doc: any): string => doc.textBetween(0, doc.content.size, BLOCK_SEPARATOR)

const limitRef = ref<number | null>(props.maxCharCount ?? null)
let bypassLimit = false

const CharLimit = Extension.create({
  name: 'charLimit',
  addProseMirrorPlugins() {
    return [
      new Plugin({
        key: new PluginKey('charLimit'),
        filterTransaction: (tr) => {
          if (bypassLimit) return true
          const max = limitRef.value
          if (!max || max <= 0) return true
          if (!tr.docChanged) return true
          const next = docText(tr.doc).length
          if (next <= max) return true
          return next < docText(tr.startState.doc).length
        },
      }),
    ]
  },
})

const charCount = ref(0)
const isEmpty = ref(true)
const internalError = ref('')

const editor = useEditor({
  content: props.modelValue,
  extensions: [TiptapStarterKit, CharLimit],
  editable: !props.disabled && !props.readonly,
  onCreate: ({ editor: instance }) => {
    charCount.value = docText(instance.state.doc).length
    isEmpty.value = instance.isEmpty
  },
  onUpdate: ({ editor: instance }) => {
    charCount.value = docText(instance.state.doc).length
    isEmpty.value = instance.isEmpty
    emit('update:modelValue', instance.getHTML())
    validate()
  },
  onBlur: ({ editor: instance }) => {
    isEmpty.value = instance.isEmpty
    validate()
  },
})

watch(
  () => props.modelValue,
  (value) => {
    const instance = editor.value
    if (!instance) return
    if (value === instance.getHTML()) return
    bypassLimit = true
    instance.commands.setContent(value || '', { emitUpdate: false })
    bypassLimit = false
    charCount.value = docText(instance.state.doc).length
    isEmpty.value = instance.isEmpty
  },
)

watch(
  () => props.maxCharCount,
  (value) => {
    limitRef.value = value ?? null
  },
)

watch(
  () => [props.disabled, props.readonly],
  ([disabled, readonly]) => {
    editor.value?.setEditable(!(disabled || readonly))
  },
)

const isPlainText = computed(() => !/<[a-z][\s\S]*>/i.test(props.modelValue || ''))

const validate = (): boolean => {
  const text = editor.value ? docText(editor.value.state.doc) : ''
  for (const rule of props.rules) {
    const result = rule(text)
    if (result !== true) {
      internalError.value = result as string
      emit('error', internalError.value)
      return false
    }
  }
  if (props.minCharCount && charCount.value < props.minCharCount) {
    internalError.value = `Minimum ${props.minCharCount} characters required.`
    emit('error', internalError.value)
    return false
  }
  internalError.value = ''
  emit('error', '')
  return true
}

const displayError = computed(() => internalError.value || props.errorMessage)
const isOverLimit = computed(() => !!props.maxCharCount && charCount.value >= props.maxCharCount)

const charCounterText = computed(() => {
  if (props.maxCharCount && props.minCharCount) {
    return `${charCount.value} / ${props.maxCharCount} character (min ${props.minCharCount})`
  } else if (props.maxCharCount) {
    return `${charCount.value} / ${props.maxCharCount} character`
  } else if (props.minCharCount) {
    return `${charCount.value} (min: ${props.minCharCount})`
  }
  return ''
})

const always = () => true

const toolbarGroups: ToolbarButtonConfig[][] = [
  [
    {
      icon: BoldIcon,
      title: 'Bold (Ctrl+B)',
      isActive: (e) => e.isActive('bold'),
      canRun: (e) => e.can().chain().focus().toggleBold().run(),
      run: (e) => e.chain().focus().toggleBold().run(),
    },
    {
      icon: ItalicIcon,
      title: 'Italic (Ctrl+I)',
      isActive: (e) => e.isActive('italic'),
      canRun: (e) => e.can().chain().focus().toggleItalic().run(),
      run: (e) => e.chain().focus().toggleItalic().run(),
    },
    {
      icon: NoSymbolIcon,
      title: 'Strikethrough',
      isActive: (e) => e.isActive('strike'),
      canRun: (e) => e.can().chain().focus().toggleStrike().run(),
      run: (e) => e.chain().focus().toggleStrike().run(),
    },
    {
      icon: CodeBracketIcon,
      title: 'Inline code',
      isActive: (e) => e.isActive('code'),
      canRun: (e) => e.can().chain().focus().toggleCode().run(),
      run: (e) => e.chain().focus().toggleCode().run(),
    },
    {
      icon: ArrowPathIcon,
      title: 'Clear formatting',
      isActive: always,
      canRun: always,
      run: (e) => e.chain().focus().unsetAllMarks().clearNodes().run(),
    },
  ],
  [
    {
      icon: H1Icon,
      title: 'Heading 1',
      isActive: (e) => e.isActive('heading', { level: 1 }),
      canRun: always,
      run: (e) => e.chain().focus().toggleHeading({ level: 1 }).run(),
    },
    {
      icon: H2Icon,
      title: 'Heading 2',
      isActive: (e) => e.isActive('heading', { level: 2 }),
      canRun: always,
      run: (e) => e.chain().focus().toggleHeading({ level: 2 }).run(),
    },
    {
      icon: H3Icon,
      title: 'Heading 3',
      isActive: (e) => e.isActive('heading', { level: 3 }),
      canRun: always,
      run: (e) => e.chain().focus().toggleHeading({ level: 3 }).run(),
    },
    {
      icon: DocumentTextIcon,
      title: 'Paragraph',
      isActive: (e) => e.isActive('paragraph'),
      canRun: always,
      run: (e) => e.chain().focus().setParagraph().run(),
    },
  ],
  [
    {
      icon: ListBulletIcon,
      title: 'Bullet list',
      isActive: (e) => e.isActive('bulletList'),
      canRun: (e) => e.can().chain().focus().toggleBulletList().run(),
      run: (e) => e.chain().focus().toggleBulletList().run(),
    },
    {
      icon: QueueListIcon,
      title: 'Numbered list',
      isActive: (e) => e.isActive('orderedList'),
      canRun: (e) => e.can().chain().focus().toggleOrderedList().run(),
      run: (e) => e.chain().focus().toggleOrderedList().run(),
    },
    {
      icon: ChatBubbleBottomCenterTextIcon,
      title: 'Blockquote',
      isActive: (e) => e.isActive('blockquote'),
      canRun: (e) => e.can().chain().focus().toggleBlockquote().run(),
      run: (e) => e.chain().focus().toggleBlockquote().run(),
    },
    {
      icon: CodeBracketIcon,
      title: 'Code block',
      isActive: (e) => e.isActive('codeBlock'),
      canRun: (e) => e.can().chain().focus().toggleCodeBlock().run(),
      run: (e) => e.chain().focus().toggleCodeBlock().run(),
    },
  ],
  [
    {
      icon: MinusIcon,
      title: 'Divider',
      isActive: always,
      canRun: always,
      run: (e) => e.chain().focus().setHorizontalRule().run(),
    },
  ],
  [
    {
      icon: ArrowUturnLeftIcon,
      title: 'Undo (Ctrl+Z)',
      isActive: always,
      canRun: (e) => e.can().chain().focus().undo().run(),
      run: (e) => e.chain().focus().undo().run(),
    },
    {
      icon: ArrowUturnRightIcon,
      title: 'Redo (Ctrl+Shift+Z)',
      isActive: always,
      canRun: (e) => e.can().chain().focus().redo().run(),
      run: (e) => e.chain().focus().redo().run(),
    },
  ],
  [
    {
      icon: TrashIcon,
      title: 'Clear all content',
      isActive: always,
      canRun: always,
      danger: true,
      run: (e) => e.commands.clearContent(),
    },
  ],
]
</script>

<style scoped>
.tiptap-content {
  padding: 0.75rem 1rem;
  font-size: 0.875rem;
  color: rgb(55 65 81);
}

.dark .tiptap-content {
  color: rgb(229 231 235);
}

.tiptap-content :deep(.ProseMirror) {
  outline: none;
}

.tiptap-content :deep(.ProseMirror > * + *) {
  margin-top: 0.5em;
}

.tiptap-content :deep(h1) {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1.3;
}

.tiptap-content :deep(h2) {
  font-size: 1.25rem;
  font-weight: 700;
  line-height: 1.4;
}

.tiptap-content :deep(h3) {
  font-size: 1.125rem;
  font-weight: 600;
  line-height: 1.5;
}

.tiptap-content :deep(ul) {
  list-style: disc;
  padding-left: 1.5rem;
}

.tiptap-content :deep(ol) {
  list-style: decimal;
  padding-left: 1.5rem;
}

.tiptap-content :deep(blockquote) {
  border-left: 3px solid rgb(203 213 225);
  padding-left: 0.75rem;
  font-style: italic;
  color: rgb(100 116 139);
}

.dark .tiptap-content :deep(blockquote) {
  border-left-color: rgb(71 85 105);
  color: rgb(148 163 184);
}

.tiptap-content :deep(code) {
  background: rgb(243 244 246);
  border-radius: 0.25rem;
  padding: 0.1rem 0.3rem;
  font-size: 0.875em;
}

.tiptap-content :deep(pre) {
  background: rgb(15 23 42);
  color: rgb(226 232 240);
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  overflow-x: auto;
  font-size: 0.875rem;
}

.tiptap-content :deep(pre code) {
  background: transparent;
  padding: 0;
  color: inherit;
}

.tiptap-content :deep(hr) {
  border: 0;
  border-top: 1px solid rgb(229 231 235);
  margin: 1rem 0;
}
</style>
