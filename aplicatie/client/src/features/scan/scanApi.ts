import { useMutation } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type ScanResultDto = Schemas['ScanResultDto']
export type ScanFieldsDto = Schemas['ScanFieldsDto']

/** Sends a label photo to POST /api/scans; the API checks its real type and size (ADR-010). */
export function useScanLabel() {
  return useMutation({
    mutationFn: async (file: File) =>
      unwrap(
        await api.POST('/api/scans', {
          body: { file: '' },
          bodySerializer: () => {
            const form = new FormData()
            form.append('file', file)
            return form
          },
        }),
      ),
  })
}
