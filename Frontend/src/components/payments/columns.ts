import { h } from 'vue'
import type { ColumnDef } from '@tanstack/vue-table'
import { saveAs } from 'file-saver';

// Define your new data interface for trading columns
export interface TradingData {
  instrument: string  // MNQ, MES, etc.
  parameters: string  // 1, 2, 3 (Slow, Fast, StopLoss, etc.)
  total_net_profit: number
  profit_factor: number
  max_drawdown: number
  num_trades: number
  percent_profitable: number
}

const greaterThanOrEqualFilter = (row: any, columnId: any, filterValue: any) => {
  const rowValue = row.getValue(columnId)
  const filterNumber = Number(filterValue)
  return rowValue >= filterNumber
}

// Define the columns
export const columns: ColumnDef<TradingData>[] = [
  {
    accessorKey: 'instrument',
    header: () => h('div', { class: 'text-left' }, 'Instrument'),
    cell: ({ row }) => h('div', { class: 'text-left font-medium' }, row.getValue('instrument') as string),
  },
  {
    accessorKey: 'parameters',
    header: () => h('div', { class: 'text-left' }, 'Parameters'),
    cell: ({ row }) => {
      const propertyValue = row.getValue('parameters') as string;  // Just use properties as a string
      return h('div', { class: 'text-left' }, propertyValue);
    },
  },
  {
    accessorKey: 'total_net_profit',
    header: () => h('div', { class: 'text-right' }, 'Total Net Profit'),
    cell: ({ row }) => {
      const netProfit = row.getValue('total_net_profit') as number  // Cast to number
      const formatted = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
      }).format(netProfit)

      return h('div', { class: 'text-right font-medium' }, formatted)
    },
    filterFn: greaterThanOrEqualFilter,
  },
  {
    accessorKey: 'profit_factor',
    header: () => h('div', { class: 'text-right' }, 'Profit Factor'),
    cell: ({ row }) => {
      const profitFactor = row.getValue('profit_factor') as number  // Cast to number
      return h('div', { class: 'text-right font-medium' }, profitFactor.toFixed(2))
    },
    filterFn: greaterThanOrEqualFilter,
  },
  {
    accessorKey: 'max_drawdown',
    header: () => h('div', { class: 'text-right' }, 'Max Drawdown'),
    cell: ({ row }) => {
      const drawdown = row.getValue('max_drawdown') as number  // Cast to number
      const formatted = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
      }).format(drawdown)

      return h('div', { class: 'text-right font-medium' }, formatted)
    },
    filterFn: greaterThanOrEqualFilter,
  },
  {
    accessorKey: 'num_trades',
    header: () => h('div', { class: 'text-right' }, '# of Trades'),
    cell: ({ row }) => {
      const numTrades = row.getValue('num_trades') as number  // Cast to number
      return h('div', { class: 'text-right font-medium' }, numTrades)
    },
    filterFn: greaterThanOrEqualFilter,
  },
  {
    accessorKey: 'percent_profitable',
    header: () => h('div', { class: 'text-right' }, '% Profitable'),
    cell: ({ row }) => {
      const percent = row.getValue('percent_profitable') as number  // Cast to number
      return h('div', { class: 'text-right font-medium' }, `${percent.toFixed(2)}%`)
    },
    filterFn: greaterThanOrEqualFilter,
  },
]

export function renameAndDownloadCSV(data: TradingData[], filename: string = 'trading_data.csv') {
  // Define the new column headers
  const newHeaders = {
    instrument: 'Instrument',
    parameters: 'Parameters',
    total_net_profit: 'Total net profit',
    profit_factor: 'Profit factor',
    max_drawdown: 'Max. drawdown',
    num_trades: 'Total # of trades',
    percent_profitable: 'Percent profitable'
  };

  // Create CSV content
  let csvContent = Object.values(newHeaders).join(',') + '\n';

  data.forEach(row => {
    csvContent += Object.keys(newHeaders)
      .map(key => row[key as keyof TradingData])
      .join(',') + '\n';
  });

  // Create Blob and download
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  saveAs(blob, filename);
}
