<template>
  <div class="items-center bg-white flex flex-col rounded h-[75vh] p-4">
    <div>
      <button
        @click="handleButtonClick('success', 'Success', 'Test')"
        class="bg-green-500 text-white px-4 py-2 rounded cursor-pointer"
      >
        Show Success</button
      ><br /><br />
      <button
        @click="handleButtonClick('error', 'Error', 'Test')"
        class="bg-red-500 text-white px-4 py-2 rounded"
      >
        Show Error</button
      ><br /><br />
      <button
        @click="handleButtonClick('warning', 'Warning', 'Test')"
        class="bg-orange-500 text-white px-4 py-2 rounded"
      >
        Show Warning</button
      ><br /><br />
      <button
        @click="handleButtonClick('info', 'Info', 'Test')"
        class="bg-blue-500 text-white px-4 py-2 rounded"
      >
        Show Info</button
      ><br /><br />
      <button
        @click="handleLoading()"
        class="bg-blue-500 text-white px-4 py-2 rounded"
      >
        Show Loading</button
      >
    </div>
  </div>
</template>

<script setup>
import { showToast, startLoading, stopLoading } from "~/stores/globalvar";

function handleButtonClick(type, title, message) {
  // showNotification(type, message, 'top-20 right-10')
  showToast({
    title: title,
    message: message,
    type: type,
    animation: "zoom",
    position: "top-right",
  });
}

function handleLoading() {
  startLoading({
    loadingText: "Uploading files",
    spinnerColor: "teal",
    showEllipsis: false,
  });
  setTimeout(() => stopLoading(), 3000);
}
</script>
