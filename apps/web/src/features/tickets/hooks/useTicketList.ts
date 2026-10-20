import { useEffect, useRef, useState } from 'react';
import { ApiError } from '../../../api/client';
import { ticketsApi } from '../../../api/tickets';
import type { PagedResult, TicketFilters, TicketListItem } from '../../../types/api';

const DEBOUNCE_MS = 300;

export const emptyFilters: TicketFilters = {
  search: '',
  status: undefined,
  priority: undefined,
  categoryId: undefined,
  customerId: undefined,
  assignedAgentId: undefined,
  unassignedOnly: false,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAtUtc',
  sortDirection: 'desc',
};

interface State {
  isLoading: boolean;
  data?: PagedResult<TicketListItem>;
  error?: string;
}

/** Owns the ticket list's filters and the data they produce. */
export function useTicketList() {
  const [filters, setFilters] = useState<TicketFilters>(emptyFilters);
  const [state, setState] = useState<State>({ isLoading: true });
  const prevFiltersRef = useRef<TicketFilters | null>(null);
  const debounceTimerRef = useRef<number | null>(null);
  const activeRequestIdRef = useRef(0);

  const load = async (queryFilters: TicketFilters) => {
    const requestId = ++activeRequestIdRef.current;
    setState((current) => ({ ...current, isLoading: true, error: undefined }));

    try {
      const data = await ticketsApi.getTickets(queryFilters);
      if (requestId === activeRequestIdRef.current) {
        setState({ isLoading: false, data, error: undefined });
      }
    } catch (error) {
      if (requestId === activeRequestIdRef.current) {
        const message = error instanceof ApiError ? error.message : 'Unable to load tickets.';
        setState((current) => ({ ...current, isLoading: false, error: message }));
      }
    }
  };

  useEffect(() => {
    const prev = prevFiltersRef.current;
    prevFiltersRef.current = filters;

    // Cancel any pending debounce timer
    if (debounceTimerRef.current !== null) {
      window.clearTimeout(debounceTimerRef.current);
      debounceTimerRef.current = null;
    }

    if (!prev) {
      // Initial mount: load immediately
      void load(filters);
      return;
    }

    const searchChanged = prev.search !== filters.search;
    const allKeys = new Set([
      ...Object.keys(prev),
      ...Object.keys(filters),
    ] as (keyof TicketFilters)[]);

    allKeys.delete('search');
    // If search changed, any automatic page reset to 1 is part of the search change
    if (searchChanged) {
      allKeys.delete('page');
    }

    // Checks if ANY non-search filter changed (including future ones like slaStatus)
    const nonSearchChanged = Array.from(allKeys).some(
      (key) => prev[key] !== filters[key],
    );

    if (searchChanged && !nonSearchChanged) {
      // Only search changed: debounce to avoid requests per keystroke
      debounceTimerRef.current = window.setTimeout(() => {
        debounceTimerRef.current = null;
        void load(filters);
      }, DEBOUNCE_MS);
    } else {
      // Non-search filter changed (or reset): immediate request
      void load(filters);
    }

    return () => {
      if (debounceTimerRef.current !== null) {
        window.clearTimeout(debounceTimerRef.current);
        debounceTimerRef.current = null;
      }
    };
  }, [filters]);

  const updateFilters = (patch: Partial<TicketFilters>) =>
    setFilters((current) => ({ ...current, ...patch, page: patch.page ?? 1 }));

  const resetFilters = () => setFilters(emptyFilters);

  return { filters, updateFilters, resetFilters, reload: () => load(filters), ...state };
}
