<template>
  <div class="container mx-auto px-4 py-8">
    <h1 class="text-3xl font-bold text-gray-800 mb-6">Modal Component</h1>
    <p class="text-lg text-gray-600 mb-8">
      A flexible modal/dialog component with various sizing, positioning, and
      behavior options.
    </p>

    <!-- Basic Usage Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Basic Usage</h2>

      <div class="mb-4">
        <button
          @click="basicModal = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Open Basic Modal
        </button>

        <Modal v-model:isOpen="basicModal">
          <template #header>
            <h3 class="text-lg font-medium leading-6 text-gray-900">
              Basic Modal
            </h3>
          </template>

          <div class="mt-2">
            <p class="text-gray-600">
              This is a basic modal with default settings.
            </p>
          </div>

          <template #footer>
            <button
              @click="basicModal = false"
              class="px-4 py-2 bg-blue-500 text-white rounded"
            >
              Close
            </button>
          </template>
        </Modal>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;button @click="basicModal = true" class="px-4 py-2 bg-blue-500 text-white rounded"&gt;
  Open Basic Modal
&lt;/button&gt;

&lt;Modal v-model:isOpen="basicModal"&gt;
  &lt;template #header&gt;
    &lt;h3 class="text-lg font-medium leading-6 text-gray-900"&gt;Basic Modal&lt;/h3&gt;
  &lt;/template&gt;
  
  &lt;div class="mt-2"&gt;
    &lt;p class="text-gray-600"&gt;This is a basic modal with default settings.&lt;/p&gt;
  &lt;/div&gt;
  
  &lt;template #footer&gt;
    &lt;button @click="basicModal = false" class="px-4 py-2 bg-blue-500 text-white rounded"&gt;
      Close
    &lt;/button&gt;
  &lt;/template&gt;
&lt;/Modal&gt;

&lt;script setup&gt;
const basicModal = ref(false);
&lt;/script&gt;</pre
        >
      </div>
    </section>

    <!-- Size Variants Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Size Variants</h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <button
          @click="sizeModals.sm = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Small Modal
        </button>
        <button
          @click="sizeModals.md = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Medium Modal
        </button>
        <button
          @click="sizeModals.lg = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Large Modal
        </button>
        <button
          @click="sizeModals.xl = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Extra Large Modal
        </button>
        <button
          @click="sizeModals.full = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Full Width Modal
        </button>
      </div>

      <Modal v-model:isOpen="sizeModals.sm" size="sm">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Small Modal
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">This is a small modal (max-width: 384px)</p>
        </div>
      </Modal>

      <Modal v-model:isOpen="sizeModals.md" size="md">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Medium Modal
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">This is a medium modal (max-width: 448px)</p>
        </div>
      </Modal>

      <Modal v-model:isOpen="sizeModals.lg" size="lg">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Large Modal
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">This is a large modal (max-width: 576px)</p>
        </div>
      </Modal>

      <Modal v-model:isOpen="sizeModals.xl" size="xl">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Extra Large Modal
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            This is an extra large modal (max-width: 640px)
          </p>
        </div>
      </Modal>

      <Modal v-model:isOpen="sizeModals.full" size="full">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Full Width Modal
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            This modal takes the full width of its container
          </p>
        </div>
      </Modal>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Small Modal --&gt;
&lt;Modal v-model:isOpen="isOpen" size="sm"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Medium Modal --&gt;
&lt;Modal v-model:isOpen="isOpen" size="md"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Large Modal --&gt;
&lt;Modal v-model:isOpen="isOpen" size="lg"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Extra Large Modal --&gt;
&lt;Modal v-model:isOpen="isOpen" size="xl"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Full Width Modal --&gt;
&lt;Modal v-model:isOpen="isOpen" size="full"&gt;
  ...
&lt;/Modal&gt;</pre
        >
      </div>
    </section>

    <!-- Position Variants Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">
        Position Variants
      </h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <button
          @click="positionModals.top = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Top Position
        </button>
        <button
          @click="positionModals.center = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Center Position
        </button>
        <button
          @click="positionModals.bottom = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Bottom Position
        </button>
        <button
          @click="positionModals.topCenter = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Top-Center Position
        </button>
        <button
          @click="positionModals.bottomCenter = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Bottom-Center Position
        </button>
      </div>

      <Modal v-model:isOpen="positionModals.top" position="top">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Top Position
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">Modal appears at the top of the screen</p>
        </div>
      </Modal>

      <Modal v-model:isOpen="positionModals.center" position="center">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Center Position
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            Modal appears in the center of the screen (default)
          </p>
        </div>
      </Modal>

      <Modal v-model:isOpen="positionModals.bottom" position="bottom">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Bottom Position
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">Modal appears at the bottom of the screen</p>
        </div>
      </Modal>

      <Modal v-model:isOpen="positionModals.topCenter" position="top-center">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Top-Center Position
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            Modal appears at the top-center of the screen
          </p>
        </div>
      </Modal>

      <Modal
        v-model:isOpen="positionModals.bottomCenter"
        position="bottom-center"
      >
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Bottom-Center Position
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            Modal appears at the bottom-center of the screen
          </p>
        </div>
      </Modal>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Top Position --&gt;
&lt;Modal v-model:isOpen="isOpen" position="top"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Center Position --&gt;
&lt;Modal v-model:isOpen="isOpen" position="center"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Bottom Position --&gt;
&lt;Modal v-model:isOpen="isOpen" position="bottom"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Top-Center Position --&gt;
&lt;Modal v-model:isOpen="isOpen" position="top-center"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Bottom-Center Position --&gt;
&lt;Modal v-model:isOpen="isOpen" position="bottom-center"&gt;
  ...
&lt;/Modal&gt;</pre
        >
      </div>
    </section>

    <!-- Behavior Options Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">
        Behavior Options
      </h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <button
          @click="behaviorModals.noCloseButton = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          No Close Button
        </button>
        <button
          @click="behaviorModals.noEsc = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Disable ESC Close
        </button>
        <button
          @click="behaviorModals.noOverlayClick = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Disable Overlay Click
        </button>
        <button
          @click="behaviorModals.allDisabled = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          All Close Options Disabled
        </button>
      </div>

      <Modal
        v-model:isOpen="behaviorModals.noCloseButton"
        :showCloseButton="false"
      >
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            No Close Button
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            This modal hides the close button in the top-right corner
          </p>
          <button
            @click="behaviorModals.noCloseButton = false"
            class="mt-4 px-4 py-2 bg-blue-500 text-white rounded"
          >
            Close Modal
          </button>
        </div>
      </Modal>

      <Modal v-model:isOpen="behaviorModals.noEsc" :closeOnEsc="false">
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            ESC Key Disabled
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">Pressing ESC will not close this modal</p>
          <button
            @click="behaviorModals.noEsc = false"
            class="mt-4 px-4 py-2 bg-blue-500 text-white rounded"
          >
            Close Modal
          </button>
        </div>
      </Modal>

      <Modal
        v-model:isOpen="behaviorModals.noOverlayClick"
        :closeOnOverlayClick="false"
      >
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            Overlay Click Disabled
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            Clicking outside the modal will not close it
          </p>
          <button
            @click="behaviorModals.noOverlayClick = false"
            class="mt-4 px-4 py-2 bg-blue-500 text-white rounded"
          >
            Close Modal
          </button>
        </div>
      </Modal>

      <Modal
        v-model:isOpen="behaviorModals.allDisabled"
        :showCloseButton="false"
        :closeOnEsc="false"
        :closeOnOverlayClick="false"
      >
        <template #header>
          <h3 class="text-lg font-medium leading-6 text-gray-900">
            All Close Options Disabled
          </h3>
        </template>
        <div class="mt-2">
          <p class="text-gray-600">
            No close button, ESC doesn't work, and clicking outside doesn't
            close
          </p>
          <button
            @click="behaviorModals.allDisabled = false"
            class="mt-4 px-4 py-2 bg-blue-500 text-white rounded"
          >
            Close Modal
          </button>
        </div>
      </Modal>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- No Close Button --&gt;
&lt;Modal v-model:isOpen="isOpen" :showCloseButton="false"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Disable ESC Close --&gt;
&lt;Modal v-model:isOpen="isOpen" :closeOnEsc="false"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- Disable Overlay Click --&gt;
&lt;Modal v-model:isOpen="isOpen" :closeOnOverlayClick="false"&gt;
  ...
&lt;/Modal&gt;

&lt;!-- All Close Options Disabled --&gt;
&lt;Modal 
  v-model:isOpen="isOpen" 
  :showCloseButton="false"
  :closeOnEsc="false"
  :closeOnOverlayClick="false"
&gt;
  ...
&lt;/Modal&gt;</pre
        >
      </div>
    </section>

    <!-- Form Example Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Form Example</h2>

      <div class="mb-4">
        <button
          @click="formModal = true"
          class="px-4 py-2 bg-blue-500 text-white rounded"
        >
          Open Form Modal
        </button>

        <Modal v-model:isOpen="formModal" size="lg">
          <template #header>
            <h3 class="text-lg font-medium leading-6 text-gray-900">
              Contact Form
            </h3>
          </template>

          <div class="mt-2">
            <form class="mx-auto grid max-w-screen-md gap-4 sm:grid-cols-2">
              <div>
                <!-- <label for="first-name" class="mb-2 inline-block text-sm text-gray-800 sm:text-base">First name</label>
                <input name="first-name" class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring" /> -->
                <BaseInput v-model="firstname" label="Firstname" />
              </div>

              <div>
                <BaseInput label="Lastname" />
              </div>

              <div class="sm:col-span-2">
                <BaseInput label="Email" />
              </div>

              <div class="sm:col-span-2">
                <BaseInput label="Message" :isTextarea="true" :rowCount="5" :maxCharCount="100" />
              </div>
            </form>
          </div>

          <template #footer>
            <div class="flex justify-between">
              <button
                @click="formModal = false"
                type="button"
                class="px-4 py-2 text-gray-700 rounded border border-gray-300 hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                class="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
              >
                Submit
              </button>
            </div>
          </template>
        </Modal>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;button @click="formModal = true" class="px-4 py-2 bg-blue-500 text-white rounded"&gt;
  Open Form Modal
&lt;/button&gt;

&lt;Modal v-model:isOpen="formModal" size="lg"&gt;
  &lt;template #header&gt;
    &lt;h3 class="text-lg font-medium leading-6 text-gray-900"&gt;Contact Form&lt;/h3&gt;
  &lt;/template&gt;
  
  &lt;div class="mt-2"&gt;
    &lt;form class="mx-auto grid max-w-screen-md gap-4 sm:grid-cols-2"&gt;
      &lt;div&gt;
        &lt;label for="first-name" class="mb-2 inline-block text-sm text-gray-800 sm:text-base"&gt;First name&lt;/label&gt;
        &lt;input name="first-name" class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring" /&gt;
      &lt;/div&gt;

      &lt;div&gt;
        &lt;label for="last-name" class="mb-2 inline-block text-sm text-gray-800 sm:text-base"&gt;Last name&lt;/label&gt;
        &lt;input name="last-name" class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring" /&gt;
      &lt;/div&gt;

      &lt;div class="sm:col-span-2"&gt;
        &lt;label for="email" class="mb-2 inline-block text-sm text-gray-800 sm:text-base"&gt;Email&lt;/label&gt;
        &lt;input name="email" class="w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring" /&gt;
      &lt;/div&gt;

      &lt;div class="sm:col-span-2"&gt;
        &lt;label for="message" class="mb-2 inline-block text-sm text-gray-800 sm:text-base"&gt;Message&lt;/label&gt;
        &lt;textarea name="message" class="h-32 w-full rounded border bg-gray-50 px-3 py-2 text-gray-800 outline-none ring-indigo-300 transition duration-100 focus:ring"&gt;&lt;/textarea&gt;
      &lt;/div&gt;
    &lt;/form&gt;
  &lt;/div&gt;
  
  &lt;template #footer&gt;
    &lt;div class="flex justify-between"&gt;
      &lt;button @click="formModal = false" type="button" class="px-4 py-2 text-gray-700 rounded border border-gray-300 hover:bg-gray-50"&gt;
        Cancel
      &lt;/button&gt;
      &lt;button type="submit" class="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"&gt;
        Submit
      &lt;/button&gt;
    &lt;/div&gt;
  &lt;/template&gt;
&lt;/Modal&gt;

&lt;script setup&gt;
const formModal = ref(false);
&lt;/script&gt;</pre
        >
      </div>
    </section>

    <!-- Props & API Reference Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">
        Props & API Reference
      </h2>

      <div class="bg-white shadow overflow-hidden sm:rounded-lg mb-6">
        <div class="px-4 py-5 sm:px-6 border-b border-gray-200">
          <h3 class="text-lg leading-6 font-medium text-gray-900">
            Modal Component Props
          </h3>
        </div>
        <div class="border-t border-gray-200">
          <dl>
            <div
              class="bg-gray-50 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">
                isOpen (required)
              </dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Boolean that controls whether the modal is open or closed. Use
                with v-model:isOpen for two-way binding.
              </dd>
            </div>
            <div
              class="bg-white px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">size</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Controls the width of the modal. Options: 'sm', 'md' (default),
                'lg', 'xl', 'full'.
              </dd>
            </div>
            <div
              class="bg-gray-50 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">position</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Controls the vertical positioning of the modal. Options: 'top',
                'center' (default), 'bottom', 'top-center', 'bottom-center'.
              </dd>
            </div>
            <div
              class="bg-white px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">
                closeOnOverlayClick
              </dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Boolean (default: true) - Whether clicking outside the modal
                closes it.
              </dd>
            </div>
            <div
              class="bg-gray-50 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">closeOnEsc</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Boolean (default: true) - Whether pressing ESC closes the modal.
              </dd>
            </div>
            <div
              class="bg-white px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">showCloseButton</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Boolean (default: true) - Whether to show the close button in
                the top-right corner.
              </dd>
            </div>
          </dl>
        </div>
      </div>

      <div class="bg-white shadow overflow-hidden sm:rounded-lg">
        <div class="px-4 py-5 sm:px-6 border-b border-gray-200">
          <h3 class="text-lg leading-6 font-medium text-gray-900">Slots</h3>
        </div>
        <div class="border-t border-gray-200">
          <dl>
            <div
              class="bg-gray-50 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">default</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                The main content of the modal.
              </dd>
            </div>
            <div
              class="bg-white px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">header</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Content for the header section (top of modal, above main
                content).
              </dd>
            </div>
            <div
              class="bg-gray-50 px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6"
            >
              <dt class="text-sm font-medium text-gray-500">footer</dt>
              <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                Content for the footer section (bottom of modal, below main
                content).
              </dd>
            </div>
          </dl>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";

const firstname = ref();

// Basic modal
const basicModal = ref(false);

// Size variants
const sizeModals = ref({
  sm: false,
  md: false,
  lg: false,
  xl: false,
  full: false,
});

// Position variants
const positionModals = ref({
  top: false,
  center: false,
  bottom: false,
  topCenter: false,
  bottomCenter: false,
});

// Behavior variants
const behaviorModals = ref({
  noCloseButton: false,
  noEsc: false,
  noOverlayClick: false,
  allDisabled: false,
});

// Form example
const formModal = ref(false);
</script>

<style>
/* Additional styles if needed */
</style>
