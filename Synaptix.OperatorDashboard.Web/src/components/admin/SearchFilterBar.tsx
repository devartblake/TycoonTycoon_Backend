'use client'

import { type ChangeEvent, useCallback, useEffect, useRef, useState } from 'react'

import Box from '@mui/material/Box'
import TextField from '@mui/material/TextField'
import InputAdornment from '@mui/material/InputAdornment'
import FormControl from '@mui/material/FormControl'
import InputLabel from '@mui/material/InputLabel'
import Select from '@mui/material/Select'
import type { SelectChangeEvent } from '@mui/material/Select'
import MenuItem from '@mui/material/MenuItem'
import Chip from '@mui/material/Chip'
import Stack from '@mui/material/Stack'

export interface FilterOption {
  label: string
  value: string
}

export interface FilterDef {
  key: string
  label: string
  options: FilterOption[]
}

export interface SearchFilterBarProps {
  searchPlaceholder?: string
  searchValue: string
  onSearchChange: (value: string) => void
  filters?: FilterDef[]
  filterValues?: Record<string, string>
  onFilterChange?: (key: string, value: string) => void
  debounceMs?: number
}

const SearchFilterBar = ({
  searchPlaceholder = 'Search...',
  searchValue,
  onSearchChange,
  filters = [],
  filterValues = {},
  onFilterChange,
  debounceMs = 300
}: SearchFilterBarProps) => {
  const [localSearch, setLocalSearch] = useState(searchValue)
  const timerRef = useRef<ReturnType<typeof setTimeout>>()

  useEffect(() => {
    setLocalSearch(searchValue)
  }, [searchValue])

  const handleSearchInput = useCallback(
    (e: ChangeEvent<HTMLInputElement>) => {
      const val = e.target.value

      setLocalSearch(val)
      clearTimeout(timerRef.current)

      timerRef.current = setTimeout(() => {
        onSearchChange(val)
      }, debounceMs)
    },
    [onSearchChange, debounceMs]
  )

  const handleFilterSelect = useCallback(
    (key: string) => (e: SelectChangeEvent) => {
      onFilterChange?.(key, e.target.value)
    },
    [onFilterChange]
  )

  const activeFilters = Object.entries(filterValues).filter(([, v]) => v !== '' && v !== undefined)

  return (
    <Box sx={{ mb: 3 }}>
      <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
        <TextField
          size='small'
          placeholder={searchPlaceholder}
          value={localSearch}
          onChange={handleSearchInput}
          sx={{ minWidth: 240 }}
          InputProps={{
            startAdornment: (
              <InputAdornment position='start'>
                <i className='ri-search-line' />
              </InputAdornment>
            )
          }}
        />
        {filters.map(filter => (
          <FormControl key={filter.key} size='small' sx={{ minWidth: 140 }}>
            <InputLabel>{filter.label}</InputLabel>
            <Select
              label={filter.label}
              value={filterValues[filter.key] ?? ''}
              onChange={handleFilterSelect(filter.key)}
            >
              <MenuItem value=''>All</MenuItem>
              {filter.options.map(opt => (
                <MenuItem key={opt.value} value={opt.value}>
                  {opt.label}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        ))}
      </Box>
      {activeFilters.length > 0 && (
        <Stack direction='row' spacing={1} sx={{ mt: 1 }}>
          {activeFilters.map(([key, value]) => {
            const filterDef = filters.find(f => f.key === key)
            const optLabel = filterDef?.options.find(o => o.value === value)?.label ?? value

            return (
              <Chip
                key={key}
                label={`${filterDef?.label ?? key}: ${optLabel}`}
                size='small'
                onDelete={() => onFilterChange?.(key, '')}
              />
            )
          })}
        </Stack>
      )}
    </Box>
  )
}

export default SearchFilterBar
