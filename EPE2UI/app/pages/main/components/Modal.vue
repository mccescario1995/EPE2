<template>
    <div class="fixed w-full h-full top-0 left-0 flex items-center justify-center z-10" v-if="open">
        <div class="absolute w-full h-full bg-gray-900 opacity-50" @click="close"></div>
        <div class="absolute max-h-full" :class="maxWidth">
            <div class="container bg-white overflow-hidden md:rounded">
                <div
                    class="px-4 py-4 leading-none flex justify-between items-center font-medium text-sm bg-gray-100 border-b select-none">
                    <h3>{{ title }}</h3>
                    <div @click="close" class="text-2xl hover:text-gray-600 cursor-pointer">
                        &#215;
                    </div>
                </div>
                <div class="max-h-full px-4 py-4">
                    <slot></slot>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>

const props = defineProps({
    title: {
        type: String,
        default: "",
    },
    header: {
        type: String,
        required: false,
        default: "",
    },
    width: {
        type: String,
        default: "full",
        validator: (value) => ["xs", "sm", "md", "lg", "full"].indexOf(value) !== -1,
    },
});

const emit = defineEmits(['close']);

const open = ref(true);

const close = () => {
    open.value = false;
    emit('close');
};

const maxWidth = computed(() => {
    switch (props.width) {
        case 'xs':
            return 'max-w-lg';
        case 'sm':
            return 'max-w-xl';
        case 'md':
            return 'max-w-3xl';
        case 'lg':
            return 'max-w-4xl';
        case 'full':
            return 'max-w-full';
    }
});

onMounted(() => {
    const onEscape = (e) => {
        if (e.key === 'Esc' || e.key === 'Escape') {
            close();
        }
    };

    document.addEventListener('keydown', onEscape);

    onBeforeUnmount(() => {
        document.removeEventListener('keydown', onEscape);
    });
});

</script>
