<script setup lang="ts" generic="TData, TValue">
import type {
    ColumnDef,
    ColumnFiltersState
} from '@tanstack/vue-table'
import {
    FlexRender,
    getCoreRowModel,
    useVueTable,
    getFilteredRowModel,
    getPaginationRowModel
} from '@tanstack/vue-table'
import { Input } from '@/components/ui/input'
import { ref } from 'vue';
import { valueUpdater } from '@/lib/utils'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { Button } from '@/components/ui/button'

const props = defineProps<{
    columns: ColumnDef<TData, TValue>[]
    data: TData[]
}>()

const columnFilters = ref<ColumnFiltersState>([])

const table = useVueTable({
    get data() { return props.data },
    get columns() { return props.columns },
    getCoreRowModel: getCoreRowModel(),
    onColumnFiltersChange: updaterOrValue => valueUpdater(updaterOrValue, columnFilters),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    state: {
        get columnFilters() { return columnFilters.value },
    },
})
</script>

<template>
    <div>
        <div class="flex items-center py-4 space-x-2">
            <Input class="w-32" placeholder="Filter Profit Factor..."
                :model-value="table.getColumn('profit_factor')?.getFilterValue() as string"
                @update:model-value="table.getColumn('profit_factor')?.setFilterValue($event)" />

            <!-- Net Profitability Filter -->
            <Input class="w-32" placeholder="Filter Total Net Profit..."
                :model-value="table.getColumn('total_net_profit')?.getFilterValue() as string"
                @update:model-value="table.getColumn('total_net_profit')?.setFilterValue($event)" />

            <!-- Percentage Profitable Filter -->
            <Input class="w-32" placeholder="Filter % Profitable..."
                :model-value="table.getColumn('percent_profitable')?.getFilterValue() as string"
                @update:model-value="table.getColumn('percent_profitable')?.setFilterValue($event)" />

            <!-- Max Drawdown Filter -->
            <Input class="w-32" placeholder="Filter Max Drawdown..."
                :model-value="table.getColumn('max_drawdown')?.getFilterValue() as string"
                @update:model-value="table.getColumn('max_drawdown')?.setFilterValue($event)" />

            <!-- Num Trade Filter -->
            <Input class="w-32" placeholder="Filter # of Trades..."
                :model-value="table.getColumn('num_trades')?.getFilterValue() as string"
                @update:model-value="table.getColumn('num_trades')?.setFilterValue($event)" />
        </div>

        <div class="border rounded-md">
            <Table>
                <TableHeader>
                    <TableRow v-for="headerGroup in table.getHeaderGroups()" :key="headerGroup.id">
                        <TableHead v-for="header in headerGroup.headers" :key="header.id">
                            <FlexRender v-if="!header.isPlaceholder" :render="header.column.columnDef.header"
                                :props="header.getContext()" />
                        </TableHead>
                    </TableRow>
                </TableHeader>
                <TableBody>
                    <template v-if="table.getRowModel().rows?.length">
                        <TableRow v-for="row in table.getRowModel().rows" :key="row.id"
                            :data-state="row.getIsSelected() ? 'selected' : undefined">
                            <TableCell v-for="cell in row.getVisibleCells()" :key="cell.id">
                                <FlexRender :render="cell.column.columnDef.cell" :props="cell.getContext()" />
                            </TableCell>
                        </TableRow>
                    </template>
                    <template v-else>
                        <TableRow>
                            <TableCell :colspan="columns.length" class="h-24 text-center">
                                No results.
                            </TableCell>
                        </TableRow>
                    </template>
                </TableBody>
            </Table>
        </div>
        <div class="flex items-center justify-end py-4 space-x-2">
            <Button variant="outline" size="sm" :disabled="!table.getCanPreviousPage()" @click="table.previousPage()">
                Previous
            </Button>
            <Button variant="outline" size="sm" :disabled="!table.getCanNextPage()" @click="table.nextPage()">
                Next
            </Button>
        </div>
    </div>
</template>