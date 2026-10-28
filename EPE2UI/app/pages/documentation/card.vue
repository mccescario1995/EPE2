<template>
  <div class="container mx-auto px-4 py-8 ">
    <h1 class="text-3xl font-bold text-gray-800 mb-6">Card Component</h1>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Overview</h2>
      <p class="text-gray-600 mb-4">
        The Card component is a flexible container for displaying content with
        optional headers, footers, and images. It supports multiple variants and
        can be customized using props or slots.
      </p>

      <div class="bg-blue-50 border-l-4 border-blue-500 p-4 mb-6">
        <h3 class="font-bold text-blue-800 mb-2">When to use</h3>
        <ul class="list-disc pl-5 text-blue-700 space-y-1">
          <li>Displaying grouped content in a visually distinct container</li>
          <li>Creating product or feature cards</li>
          <li>Building dashboard widgets</li>
          <li>Presenting image galleries with captions</li>
        </ul>
      </div>
    </section>

    <!-- Basic Cards Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Basic Cards</h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-4">
        <card
          title="Basic Card"
          content="This is a simple card with default styling."
        />

        <card
          title="With Badge"
          content="Card with an informational badge."
          badge="New"
          badgeType="info"
        />
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Basic Card --&gt;
&lt;card
  title="Basic Card"
  content="This is a simple card with default styling."
/&gt;

&lt;!-- With Badge --&gt;
&lt;card
  title="With Badge"
  content="Card with an informational badge."
  badge="New"
  badgeType="info"
/&gt;</pre
        >
      </div>
    </section>

    <!-- Image Cards Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Image Cards</h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-4">
        <card
          title="Image Card"
          content="Card with image and hover effect."
          image="https://images.unsplash.com/photo-1633356122544-f134324a6cee?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=1170&q=80"
          imageAlt="Abstract background"
        />

        <card
          title="Image with Overlay"
          content="Card with image overlay effect."
          image="https://images.unsplash.com/photo-1633356122544-f134324a6cee?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=1170&q=80"
          imageAlt="Abstract background"
          imageOverlay
        />
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Image Card --&gt;
&lt;card
  title="Image Card"
  content="Card with image and hover effect."
  image="/path/to/image.jpg"
  imageAlt="Description of image"
/&gt;

&lt;!-- Image with Overlay --&gt;
&lt;card
  title="Image with Overlay"
  content="Card with image overlay effect."
  image="/path/to/image.jpg"
  imageAlt="Description of image"
  imageOverlay
/&gt;</pre
        >
      </div>
    </section>

    <!-- Action Cards Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Action Cards</h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-4">
        <card
          title="Primary Action"
          content="Card with primary action button."
          actionText="Learn more"
          showActions
        />

        <card
          title="Dual Actions"
          content="Card with primary and secondary actions."
          actionText="Details"
          secondaryAction="Save"
          showActions
        />
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Single Action --&gt;
&lt;card
  title="Primary Action"
  content="Card with primary action button."
  actionText="Learn more"
  showActions
/&gt;

&lt;!-- Dual Actions --&gt;
&lt;card
  title="Dual Actions"
  content="Card with primary and secondary actions."
  actionText="Details"
  secondaryAction="Save"
  showActions
/&gt;</pre
        >
      </div>
    </section>

    <!-- Slot Cards Section -->
    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">
        Custom Slot Cards
      </h2>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 mb-4">
        <card>
          <template #title>Custom Title Slot</template>
          <p class="text-gray-600">
            This content is passed through the default slot.
          </p>
          <template #footer>
            <div class="flex justify-between items-center">
              <button class="text-sm text-blue-600 hover:text-blue-800">
                Cancel
              </button>
              <button
                class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Confirm
              </button>
            </div>
          </template>
        </card>

        <card>
          <template #image>
            <div
              class="h-48 bg-gradient-to-r from-green-400 to-blue-500 flex items-center justify-center"
            >
              <span class="text-white text-xl font-bold"
                >Custom Image Slot</span
              >
            </div>
          </template>
          <p>This card uses a custom image slot instead of an image prop.</p>
        </card>
      </div>

      <div class="bg-gray-50 p-4 rounded-lg mb-8">
        <h4 class="font-mono text-sm mb-2">Example Code:</h4>
        <pre
          class="bg-gray-800 text-gray-100 p-4 rounded text-sm overflow-x-auto"
        >
&lt;!-- Custom Title and Footer --&gt;
&lt;card&gt;
  &lt;template #title&gt;Custom Title Slot&lt;/template&gt;
  &lt;p class="text-gray-600"&gt;This content is passed through the default slot.&lt;/p&gt;
  &lt;template #footer&gt;
    &lt;div class="flex justify-between items-center"&gt;
      &lt;button class="text-sm text-blue-600 hover:text-blue-800"&gt;Cancel&lt;/button&gt;
      &lt;button class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"&gt;Confirm&lt;/button&gt;
    &lt;/div&gt;
  &lt;/template&gt;
&lt;/card&gt;

&lt;!-- Custom Image Slot --&gt;
&lt;card&gt;
  &lt;template #image&gt;
    &lt;div class="h-48 bg-gradient-to-r from-green-400 to-blue-500"&gt;
      &lt;span class="text-white text-xl font-bold"&gt;Custom Image Slot&lt;/span&gt;
    &lt;/div&gt;
  &lt;/template&gt;
  &lt;p&gt;This card uses a custom image slot instead of an image prop.&lt;/p&gt;
&lt;/card&gt;</pre
        >
      </div>
    </section>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Props</h2>
      <div class="overflow-x-auto">
        <table class="min-w-full bg-white border border-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Prop
              </th>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Type
              </th>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Default
              </th>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Description
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                title
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                'Card Title'
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                The title displayed in the card header
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                content
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                'Card content...'
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Main content of the card
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                image
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                ''
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                URL of the image to display
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                imageAlt
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                'Card image'
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Alt text for the image
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                imageOverlay
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                Boolean
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                false
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Adds a gradient overlay to the image
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                badge
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                ''
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Text to display in the badge
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                badgeType
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                'default'
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                One of: 'default', 'info', 'success', 'warning', 'danger'
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                actionText
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                ''
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Text for the primary action link
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                actionType
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                'primary'
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                One of: 'primary', 'secondary', 'text'
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                secondaryAction
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                String
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                ''
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Text for the secondary action button
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                hoverEffect
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                Boolean
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                true
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Enables hover effects (shadow and lift)
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                fullWidth
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                Boolean
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                false
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Removes max-width constraint
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                showActions
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                Boolean
              </td>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                false
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Shows action buttons when using props
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="mb-12">
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Slots</h2>
      <div class="overflow-x-auto">
        <table class="min-w-full bg-white border border-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Slot
              </th>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Description
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                default
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Main content of the card (replaces the content prop)
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                title
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Custom title content (replaces the title prop)
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                image
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Custom image content (replaces the image prop)
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                footer
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Footer content with actions (replaces action buttons when using
                props)
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section>
      <h2 class="text-2xl font-semibold text-gray-700 mb-4">Events</h2>
      <div class="overflow-x-auto">
        <table class="min-w-full bg-white border border-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Event
              </th>
              <th
                class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
              >
                Description
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                action
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Emitted when primary action is clicked
              </td>
            </tr>
            <tr>
              <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                secondary-action
              </td>
              <td class="px-6 py-4 text-sm text-gray-500">
                Emitted when secondary action is clicked
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<script setup>
import Card from "@/components/Card.vue";
</script>
