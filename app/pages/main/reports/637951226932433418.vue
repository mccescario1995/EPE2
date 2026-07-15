<script setup lang="ts">
definePageMeta({
  middleware: "auth",
});

navigateTo("/main/reoports/closed", { replace: true });
</script>

<template>
  <div></div>
</template>
