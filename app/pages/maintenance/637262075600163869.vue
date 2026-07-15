<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

navigateTo("/maintenance/policies", { replace: true });
</script>

<template>
  <div></div>
</template>
