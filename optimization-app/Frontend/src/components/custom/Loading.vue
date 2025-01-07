<template>
    <div class="loading-screen" :class="{ 'fade-out': !isVisible }">
        <!-- Replace SVG with Lucide Rocket component -->
        <Rocket class="rocket-icon" />
    </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted } from 'vue';
import { Rocket } from 'lucide-vue-next';

export default defineComponent({
    name: 'Loading',
    components: {
        Rocket,
    },
    setup() {
        const isVisible = ref(true);

        // Simulate loading with a timeout
        onMounted(() => {
            setTimeout(() => {
                isVisible.value = false; // Start fade-out transition after 5 seconds
            }, 5000); // 5000 milliseconds = 5 seconds
        });

        return { isVisible };
    },
});
</script>

<style scoped>
.loading-screen {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: hsl(var(--background));
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
    opacity: 1;
    transition: opacity 1s ease;
}

.loading-screen.fade-out {
    opacity: 0;
    pointer-events: none;
}

.rocket-icon {
    width: 100px;
    height: 100px;
    color: #3498db;
    animation: shake 0.5s ease-in-out infinite alternate, launch 1s linear infinite;
}

@keyframes shake {
    0% {
        transform: translate(0);
    }

    100% {
        transform: translate(-5px, 5px);
    }
}

@keyframes launch {
    0% {
        transform: translateY(0);
    }

    50% {
        transform: translateY(-15px);
    }

    100% {
        transform: translateY(0);
    }
}
</style>
