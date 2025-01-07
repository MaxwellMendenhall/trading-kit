<template>
  <div id="app">
    <Toaster />
    <Loading v-if="isLoading" />
    <Layout />
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted } from 'vue';
import Loading from './components/custom/Loading.vue';
import Layout from '@/components/custom/Layout.vue'; 
import { useColorMode } from '@vueuse/core'
import Toaster from '@/components/ui/toast/Toaster.vue'

export default defineComponent({
  name: 'App',
  components: {
    Loading,
    Layout,
    Toaster
  },
  setup() {
    const isLoading = ref<boolean>(true);

    const mode = useColorMode()
    mode.value = 'dark';

    onMounted(() => {
      // Show loading for 5 seconds before loading the main content
      setTimeout(() => {
        isLoading.value = false;
      }, 5000);
    });

    return { isLoading, mode };
  },
});
</script>

<style>
/* Add any global styles here */
</style>
