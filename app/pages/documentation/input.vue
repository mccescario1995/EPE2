<!-- pages/InputDocu.vue -->
<template>
  <div class="p-6 space-y-8">
    <!-- Title -->
    <h1 class="text-2xl font-bold">📘 BaseInput Documentation</h1>
    <p class="text-gray-600">
      The <code class="bg-gray-100 px-1 rounded">BaseInput</code> component is a
      reusable input component with support for text, password, textarea,
      validation rules, character count, and custom styling.
    </p>

    <!-- Props Section -->
    <section>
      <h2 class="text-xl font-semibold mt-6 mb-2">Props</h2>
      <table class="min-w-full border text-sm">
        <thead class="bg-gray-100">
          <tr>
            <th class="border px-3 py-2 text-left">Prop</th>
            <th class="border px-3 py-2 text-left">Type</th>
            <th class="border px-3 py-2 text-left">Default</th>
            <th class="border px-3 py-2 text-left">Description</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td class="border px-3 py-2">modelValue</td>
            <td class="border px-3 py-2">string | number | null</td>
            <td class="border px-3 py-2">""</td>
            <td class="border px-3 py-2">The value of the input (v-model).</td>
          </tr>
          <tr>
            <td class="border px-3 py-2">label</td>
            <td class="border px-3 py-2">string</td>
            <td class="border px-3 py-2">—</td>
            <td class="border px-3 py-2">Optional label text.</td>
          </tr>
          <tr>
            <td class="border px-3 py-2">type</td>
            <td class="border px-3 py-2">string</td>
            <td class="border px-3 py-2">"text"</td>
            <td class="border px-3 py-2">
              Input type (text, password, number, etc).
            </td>
          </tr>
          <tr>
            <td class="border px-3 py-2">isTextarea</td>
            <td class="border px-3 py-2">boolean</td>
            <td class="border px-3 py-2">false</td>
            <td class="border px-3 py-2">
              Render a textarea instead of input.
            </td>
          </tr>
          <tr>
            <td class="border px-3 py-2">rowCount</td>
            <td class="border px-3 py-2">number</td>
            <td class="border px-3 py-2">3</td>
            <td class="border px-3 py-2">Number of rows for textarea.</td>
          </tr>
          <tr>
            <td class="border px-3 py-2">maxCharCount</td>
            <td class="border px-3 py-2">number</td>
            <td class="border px-3 py-2">—</td>
            <td class="border px-3 py-2">
              Maximum characters (shows counter).
            </td>
          </tr>
          <tr>
            <td class="border px-3 py-2">rules</td>
            <td class="border px-3 py-2">function[]</td>
            <td class="border px-3 py-2">[]</td>
            <td class="border px-3 py-2">
              Array of validation rules (return true | string).
            </td>
          </tr>
          <tr>
            <td class="border px-3 py-2">showIcon / hideIcon</td>
            <td class="border px-3 py-2">Vue component</td>
            <td class="border px-3 py-2">—</td>
            <td class="border px-3 py-2">Custom password toggle icons.</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- Events Section -->
    <section>
      <h2 class="text-xl font-semibold mt-6 mb-2">Events</h2>
      <ul class="list-disc pl-6 text-sm text-gray-700">
        <li>
          <code>update:modelValue</code> — emitted when input value changes.
        </li>
        <li>
          <code>error</code> — emitted with error message when validation fails.
        </li>
      </ul>
    </section>

    <!-- Examples Section -->
    <section>
      <h2 class="text-xl font-semibold mt-6 mb-2">Usage Examples</h2>

      <!-- Example 1 -->
      <div class="mb-4">
        <h3 class="font-semibold">Basic Text Input</h3>
        <BaseInput v-model="exampleText" label="Name" />
        <pre class="bg-gray-100 p-2 mt-2 text-xs rounded">
&lt;BaseInput v-model="exampleText" label="Name" /&gt;
        </pre>
      </div>

      <!-- Example 2 -->
      <div class="mb-4">
        <h3 class="font-semibold">Password Input with Toggle</h3>
        <BaseInput v-model="examplePassword" label="Password" type="password" />
        <pre class="bg-gray-100 p-2 mt-2 text-xs rounded">
&lt;BaseInput v-model="examplePassword" label="Password" type="password" /&gt;
        </pre>
      </div>

      <!-- Example 3 -->
      <div class="mb-4">
        <h3 class="font-semibold">Textarea with Char Counter</h3>
        <BaseInput
          v-model="exampleMessage"
          label="Message"
          :isTextarea="true"
          :maxCharCount="100"
        />
        <pre class="bg-gray-100 p-2 mt-2 text-xs rounded">
&lt;BaseInput
  v-model="exampleMessage"
  label="Message"
  :isTextarea="true"
  :maxCharCount="100"
/&gt;
        </pre>
      </div>

      <!-- Example 4 -->
      <div class="mb-4">
        <h3 class="font-semibold">Validation Rules</h3>
        <BaseInput
          v-model="exampleEmail"
          label="Email"
          :rules="[
            (v) => !!v || 'Required',
            (v) => /.+@.+\..+/.test(v) || 'Invalid email',
          ]"
        />
        <pre class="bg-gray-100 p-2 mt-2 text-xs rounded">
&lt;BaseInput
  v-model="exampleEmail"
  label="Email"
  :rules="[
    v =&gt; !!v || 'Required',
    v =&gt; /.+@.+\..+/.test(v) || 'Invalid email'
  ]"
/&gt;
        </pre>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import BaseInput from "@/components/BaseInput.vue";
import { ref } from "vue";

// Example states
const exampleText = ref("");
const examplePassword = ref("");
const exampleMessage = ref("");
const exampleEmail = ref("");
</script>
