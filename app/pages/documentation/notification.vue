<template>
  <div class="p-8 max-w-4xl mx-auto ">
    <h1 class="text-3xl font-bold mb-6">Notification Component</h1>

    <section class="mb-8">
      <h2 class="text-2xl font-semibold mb-4">Introduction</h2>
      <p class="mb-4">
        The Notification component provides customizable toast notifications
        that can be triggered from anywhere in your application.
      </p>

      <div class="bg-blue-50 border-l-4 border-blue-500 p-4 mb-6">
        <h3 class="font-bold mb-2">Key Features:</h3>
        <ul class="list-disc pl-5 space-y-1">
          <li>Multiple notification types (success, error, warning, info)</li>
          <li>Customizable positioning (top/bottom, left/right/center)</li>
          <li>Animation effects (fade, slide, zoom)</li>
          <li>Auto-dismiss after timeout</li>
          <li>Loading indicators with progress</li>
          <li>Programmatic API for easy triggering</li>
        </ul>
      </div>
    </section>

    <!-- Basic Usage -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Basic Usage</h2>
      <p class="mb-4">
        Trigger notifications using the <code>showToast</code> function:
      </p>

      <div class="grid grid-cols-2 gap-4 mb-6">
        <button
          @click="showExample('success')"
          class="bg-green-500 hover:bg-green-600 text-white px-4 py-2 rounded"
        >
          Success Notification
        </button>
        <button
          @click="showExample('error')"
          class="bg-red-500 hover:bg-red-600 text-white px-4 py-2 rounded"
        >
          Error Notification
        </button>
        <button
          @click="showExample('warning')"
          class="bg-orange-500 hover:bg-orange-600 text-white px-4 py-2 rounded"
        >
          Warning Notification
        </button>
        <button
          @click="showExample('info')"
          class="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded"
        >
          Info Notification
        </button>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
// Import the showToast function
import { showToast } from '~/stores/globalvar'

// Basic notification
showToast({
  title: 'Success!',
  message: 'Your action was completed successfully',
  type: 'success'
})

// Error with custom duration
showToast({
  title: 'Error',
  message: 'Something went wrong',
  type: 'error',
  duration: 5000 // 5 seconds
})</pre
        >
      </div>
    </section>

    <!-- Positioning Options -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Positioning</h2>
      <p class="mb-4">
        Notifications can be positioned in different screen locations:
      </p>

      <div class="grid grid-cols-3 gap-4 mb-6">
        <button
          @click="showPositioned('top-left')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Top Left
        </button>
        <button
          @click="showPositioned('top-center')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Top Center
        </button>
        <button
          @click="showPositioned('top-right')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Top Right
        </button>
        <button
          @click="showPositioned('bottom-left')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Bottom Left
        </button>
        <button
          @click="showPositioned('bottom-center')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Bottom Center
        </button>
        <button
          @click="showPositioned('bottom-right')"
          class="bg-gray-200 hover:bg-gray-300 px-4 py-2 rounded"
        >
          Bottom Right
        </button>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
// Position examples
showToast({
  title: 'Top Right',
  message: 'Default position',
  type: 'info',
  position: 'top-right'
})

showToast({
  title: 'Bottom Center', 
  message: 'Centered at bottom',
  type: 'success',
  position: 'bottom-center'
})</pre
        >
      </div>
    </section>

    <!-- Animations -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Animations</h2>
      <p class="mb-4">Choose from different enter/exit animations:</p>

      <div class="grid grid-cols-3 gap-4 mb-6">
        <button
          @click="showAnimated('shake')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Shake
        </button>
        <button
          @click="showAnimated('bounce')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Bounce
        </button>
        <button
          @click="showAnimated('flip')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Flip
        </button>
        <button
          @click="showAnimated('zoom')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Zoom
        </button>
        <button
          @click="showAnimated('swing')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Swing
        </button>
        <button
          @click="showAnimated('tada')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Tada
        </button>
        <button
          @click="showAnimated('wobble')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Wobble
        </button>
        <button
          @click="showAnimated('pulse')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          Pulse
        </button>
        <button
          @click="showAnimated('none')"
          class="bg-purple-100 hover:bg-purple-200 px-4 py-2 rounded"
        >
          None
        </button>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
// Animation examples
showToast({
  title: 'Shake Animation',
  message: 'Smooth shake effect',
  type: 'info',
  animation: 'shake'
})

showToast({
  title: 'Bounce Animation',
  message: 'Smooth bounce effect',
  type: 'warning',
  animation: 'bounce'
})</pre
        >
      </div>
    </section>

    <!-- Loading Indicators -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">Loading Indicators</h2>
      <p class="mb-4">Show loading progress for async operations:</p>

      <div class="flex gap-4 mb-6">
        <button
          @click="showLoading('basic')"
          class="bg-teal-500 hover:bg-teal-600 text-white px-4 py-2 rounded"
        >
          Basic Loading
        </button>
        <button
          @click="showLoading('custom')"
          class="bg-teal-500 hover:bg-teal-600 text-white px-4 py-2 rounded"
        >
          Custom Loading
        </button>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
// Import loading functions
import { startLoading, stopLoading } from '~/stores/globalvar'

// Basic loading
startLoading()

// Custom loading
startLoading({
  loadingText: 'Processing your request',
  spinnerColor: 'blue',
  showEllipsis: true
})

// Stop loading when done
stopLoading()</pre
        >
      </div>
    </section>

    <!-- API Reference -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold mb-4">API Reference</h2>

      <h3 class="text-xl font-medium mb-3">showToast(options)</h3>
      <div class="overflow-x-auto mb-6">
        <table class="min-w-full border">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left">Option</th>
              <th class="px-4 py-2 text-left">Type</th>
              <th class="px-4 py-2 text-left">Default</th>
              <th class="px-4 py-2 text-left">Description</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-4 py-2 font-mono text-sm">title</td>
              <td class="px-4 py-2">String</td>
              <td class="px-4 py-2">''</td>
              <td class="px-4 py-2">Notification title text</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">message</td>
              <td class="px-4 py-2">String</td>
              <td class="px-4 py-2">''</td>
              <td class="px-4 py-2">Main notification content</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">type</td>
              <td class="px-4 py-2">
                'success' | 'error' | 'warning' | 'info'
              </td>
              <td class="px-4 py-2">'info'</td>
              <td class="px-4 py-2">Determines color and icon</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">duration</td>
              <td class="px-4 py-2">Number</td>
              <td class="px-4 py-2">3000</td>
              <td class="px-4 py-2">Display time in milliseconds</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">position</td>
              <td class="px-4 py-2">String</td>
              <td class="px-4 py-2">'top-right'</td>
              <td class="px-4 py-2">Screen position for notification</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">animation</td>
              <td class="px-4 py-2">'fade' | 'slide' | 'zoom'</td>
              <td class="px-4 py-2">'fade'</td>
              <td class="px-4 py-2">Enter/exit animation style</td>
            </tr>
          </tbody>
        </table>
      </div>

      <h3 class="text-xl font-medium mb-3">Loading Functions</h3>
      <div class="overflow-x-auto">
        <table class="min-w-full border">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-2 text-left">Function</th>
              <th class="px-4 py-2 text-left">Parameters</th>
              <th class="px-4 py-2 text-left">Description</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-4 py-2 font-mono text-sm">startLoading</td>
              <td class="px-4 py-2">
                <div class="font-mono text-sm">options: {</div>
                <div class="font-mono text-sm ml-4">loadingText?: string</div>
                <div class="font-mono text-sm ml-4">spinnerColor?: string</div>
                <div class="font-mono text-sm ml-4">showEllipsis?: boolean</div>
                <div class="font-mono text-sm">}</div>
              </td>
              <td class="px-4 py-2">Shows a loading indicator</td>
            </tr>
            <tr>
              <td class="px-4 py-2 font-mono text-sm">stopLoading</td>
              <td class="px-4 py-2">None</td>
              <td class="px-4 py-2">Hides the loading indicator</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import { showToast, startLoading, stopLoading } from "~/stores/globalvar";

function showExample(type) {
  const titles = {
    success: "Success!",
    error: "Error",
    warning: "Warning",
    info: "Information",
  };

  const messages = {
    success: "Your action was completed successfully",
    error: "Something went wrong. Please try again.",
    warning: "This action requires your attention",
    info: "Here is some information you should know",
  };

  showToast({
    title: titles[type],
    message: messages[type],
    type: type,
  });
}

function showPositioned(position) {
  showToast({
    title: position.replace("-", " "),
    message: `This notification appears at the ${position}`,
    type: "info",
    position: position,
  });
}

function showAnimated(animation) {
  showToast({
    title: `${animation} Animation`,
    message: `This uses the ${animation} animation effect`,
    type: "info",
    animation: animation,
  });
}

function showLoading(type) {
  if (type === "basic") {
    startLoading();
    setTimeout(() => stopLoading(), 3000);
  } else {
    startLoading({
      loadingText: "Processing your files...",
      spinnerColor: "purple",
      showEllipsis: true,
    });
    setTimeout(() => stopLoading(), 3000);
  }
}
</script>

<style scoped>
code {
  background-color: #f3f4f6; /* Tailwind bg-gray-100 */
  padding-left: 0.25rem; /* Tailwind px-1 */
  padding-right: 0.25rem;
  padding-top: 0.125rem; /* Tailwind py-0.5 */
  padding-bottom: 0.125rem;
  border-radius: 0.25rem; /* Tailwind rounded */
  font-size: 0.875rem; /* Tailwind text-sm */
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas,
    "Liberation Mono", "Courier New", monospace; /* Tailwind font-mono */
}
</style>
