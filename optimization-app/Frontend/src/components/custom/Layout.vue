<template>
    <div class="flex h-screen">
        <!-- Left Sidebar (30% Width) -->
        <div class="w-1/3 p-8 relative h-full">
            <!-- Top Section: Inputs -->
            <div>
                <h2 class="text-xl font-semibold mb-4">File Inputs</h2>

                <form @submit.prevent="onSubmit">
                    <!-- Dynamic Inputs -->
                    <div v-for="(_, index) in inputs" :key="index" class="mb-2 flex items-center space-x-2">
                        <FormField v-slot="{ field }" :name="`files.${index}.file`">
                            <FormItem>
                                <FormControl>
                                    <Input v-model="inputs[index].value" type="file" :placeholder="`Input ${index + 1}`"
                                        @change="e => field.onChange(e.target.files[0])" class="w-full" />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        </FormField>
                        <Button @click.prevent="removeInput(index)" variant="destructive">
                            Remove
                        </Button>
                    </div>

                    <!-- Add Input Button -->
                    <Button @click.prevent="addInput" class="mt-4 px-4 py-2" variant="secondary">
                        Add Input
                    </Button>

                    <Button type="submit" class="mt-4 ml-4">Submit</Button>

                    <Button @click.prevent="clearAllFiles" class="mt-4 px-4 py-2 ml-4" variant="secondary">
                        Clear All
                    </Button>
                </form>
            </div>

            <!-- Bottom Right Section: Social Icons -->
            <div class="absolute bottom-8 left-8 flex space-x-4">
                <Drawer>
                    <DrawerTrigger as-child>
                        <Button variant="outline">
                            How to use
                        </Button>
                    </DrawerTrigger>
                    <DrawerContent>
                        <div class="mx-auto w-full max-w-2xl p-6">
                            <DrawerHeader>
                                <DrawerTitle class="text-2xl font-bold">Welcome!</DrawerTitle>
                                <DrawerDescription class="text-sm">Quick guide on how to use this sweet
                                    free app!</DrawerDescription>
                            </DrawerHeader>
                            <div class="mt-4">
                                <p class="pb-4">
                                    NinjaTrader offers excellent optimization tools; however, navigating through each
                                    optimization run to pinpoint the desired parameters can be quite challenging. This
                                    app simplifies that process by taking care of the searching for you!
                                </p>
                                <p class="mt-2 pb-4 font-semibold">
                                    Here's a super simple guide on how to use Ninja Trader Binder:
                                </p>
                                <ol class="list-decimal list-inside mt-2">
                                    <li>
                                        Run your optimization test on NinjaTrader, either using the genetic or default
                                        settings.
                                    </li>
                                    <li>
                                        After obtaining the results, right-click on them and select the export option.
                                        Be sure to save the file as a CSV (it might save as a CSV by default sometimes)!
                                    </li>
                                    <li>
                                        Add as many file input sections as you desire. Upload your optimization results,
                                        and the app will combine everything into one file.
                                    </li>
                                    <li>
                                        This combined file will allow you to filter for specific results among your
                                        optimization data.
                                    </li>
                                </ol>
                            </div>
                            <DrawerFooter class="mt-4">
                                <DrawerClose as-child>
                                    <Button variant="outline" class="w-full">
                                        Cancel
                                    </Button>
                                </DrawerClose>
                            </DrawerFooter>
                        </div>
                    </DrawerContent>
                </Drawer>
            </div>

            <!-- Bottom Right Section: Social Icons -->
            <div class="absolute bottom-8 right-8 flex space-x-4">
                <HoverCard>
                    <HoverCardTrigger>
                        <a href="https://github.com/MaxwellMendenhall" target="_blank">
                            <Github class="w-6 h-6 text-white-500 hover:text-gray-900 cursor-pointer" />
                        </a>
                    </HoverCardTrigger>
                    <HoverCardContent>
                        GitHub social
                    </HoverCardContent>
                </HoverCard>

                <HoverCard>
                    <HoverCardTrigger>
                        <a href="https://www.linkedin.com/in/maxwell-mendenhall-317ba61b6/" target="_blank">
                            <Linkedin class="w-6 h-6 text-white-500 hover:text-gray-900 cursor-pointer" />
                        </a>
                    </HoverCardTrigger>
                    <HoverCardContent>
                        LinkedIn social
                    </HoverCardContent>
                </HoverCard>

                <HoverCard>
                    <HoverCardTrigger>
                        <a href="https://www.buymeacoffee.com/maxwell_mendenhall" target="_blank">
                            <Coffee class="w-6 h-6 text-white-500 hover:text-gray-900 cursor-pointer" />
                        </a>
                    </HoverCardTrigger>
                    <HoverCardContent>
                        Buy me a coffee!
                    </HoverCardContent>
                </HoverCard>
            </div>
        </div>
        <div class="pt-8 pb-8">
            <Separator orientation="vertical" />
        </div>

        <!-- Right Side (70% Width) -->
        <div class="w-2/3 flex flex-col h-full">
            <!-- Top Section (70% Height) -->
            <div class="flex-grow p-8 overflow-auto">
                <DataTable :columns="columns" :data="data" />
            </div>

            <div class="pr-8 pl-8">
                <Separator />
            </div>

            <!-- Bottom Section (30% Height) -->
            <div class="h-1/6 pr-8 pl-8 flex items-center justify-between">
                <Button @click="downloadCSV" class="px-4 py-2 ml-4">Download</Button>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue';
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Github, Linkedin, Coffee } from 'lucide-vue-next';
import {
    HoverCard,
    HoverCardContent,
    HoverCardTrigger,
} from '@/components/ui/hover-card'
import { toTypedSchema } from '@vee-validate/zod'
import * as z from 'zod'
import { useForm } from 'vee-validate'
import {
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage
} from '@/components/ui/form'
import { columns, renameAndDownloadCSV } from '@/components/payments/columns'
import type { TradingData } from '@/components/payments/columns'
import DataTable from '@/components/payments/data-table.vue'
import { useToast } from '@/components/ui/toast/use-toast'
import {
    Drawer,
    DrawerClose,
    DrawerContent,
    DrawerDescription,
    DrawerFooter,
    DrawerHeader,
    DrawerTitle,
    DrawerTrigger,
} from '@/components/ui/drawer'

export default defineComponent({
    name: 'Layout',
    components: {
        Button,
        Input,
        Separator,
        Github,
        Coffee,
        Linkedin,
        HoverCard,
        HoverCardContent,
        HoverCardTrigger,
        FormControl,
        FormDescription,
        FormField,
        FormItem,
        FormLabel,
        FormMessage,
        DataTable,
        columns,
        Drawer,
        DrawerClose,
        DrawerContent,
        DrawerDescription,
        DrawerFooter,
        DrawerHeader,
        DrawerTitle,
        DrawerTrigger,
    },
    setup() {
        // Manage dynamic input list
        const inputs = ref([{ value: '' }]);
        const data = ref<TradingData[]>([])
        const backend = import.meta.env.VITE_BACKEND;
        const { toast } = useToast()

        // Function to add new input
        const addInput = () => {
            inputs.value.push({ value: '' });
        };

        const removeInput = (index: number) => {
            if (inputs.value.length > 1) {
                inputs.value.splice(index, 1);
            }
        };

        const formSchema = toTypedSchema(z.object({
            files: z.array(z.object({
                file: z.instanceof(File).refine(file => file.type === 'text/csv', {
                    message: 'File must be a CSV',
                }).optional(),
            })),
        }))

        const form = useForm({
            validationSchema: formSchema,
            initialValues: {
                files: [],
            }
        })

        function downloadCSV() {
            if (data.value.length > 0) {
                renameAndDownloadCSV(data.value, 'trading_data.csv');
            } else {
                toast({
                    title: 'Error',
                    description: 'No data available to download',
                });
            }
        }

        const onSubmit = form.handleSubmit(async (values) => {
            try {
                console.log(values);

                const formData = new FormData();

                values.files.forEach((fileWrapper, _) => {
                    if (fileWrapper.file) {
                        formData.append('files', fileWrapper.file);
                    }
                });

                const url = backend + "/submit";
                const endpoint = url
                const response = await fetch(endpoint, {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    form.resetForm();
                    const responseData = await response.json();
                    data.value = responseData.data;

                    console.log(responseData.data)
                    toast({
                        title: 'Success!',
                        description: 'Files combined & ready!',
                    });
                }

            } catch (error) {
                if (error instanceof Error) {
                    toast({
                        title: 'Error!',
                        description: `Posting error. ${error}`,
                    });
                }
                toast({
                    title: 'Error!',
                    description: `Posting error. ${error}`,
                });
            }
        })

        const clearAllFiles = () => {
            inputs.value = inputs.value.map(() => ({ value: '' }))
        }

        return { inputs, addInput, removeInput, onSubmit, data, columns, clearAllFiles, downloadCSV };
    },
    data() {
        return {}
    }
});
</script>
