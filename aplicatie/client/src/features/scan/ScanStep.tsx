import { useRef, useState } from 'react'
import { Camera, ImageUp, Loader2 } from 'lucide-react'
import { problemMessage } from '@/api/problem'
import { FormError } from '@/components/ui/form'
import { useScanLabel, type ScanFieldsDto } from './scanApi'

const MAX_BYTES = 10 * 1024 * 1024

/**
 * FR-03: takes a label photo (the phone's camera via capture="environment", or a file) and sends it to the API.
 * Whatever the OCR returns, the next step is the "Add food" form: prefilled when the label was read, empty with the
 * server's message while OCR is unavailable (NFR-12, ADR-010).
 */
export function ScanStep({ onResult, onManual }: { onResult: (prefill: ScanFieldsDto | undefined, notice: string | null) => void; onManual: () => void }) {
  const input = useRef<HTMLInputElement>(null)
  const scan = useScanLabel()
  // Checked before uploading: no point sending 30 MB over 4G to hear "too large" (the server checks again).
  const [tooLarge, setTooLarge] = useState(false)

  const onFile = (file: File | undefined) => {
    if (!file) {
      return
    }
    scan.reset()
    setTooLarge(file.size > MAX_BYTES)
    if (file.size > MAX_BYTES) {
      input.current!.value = ''
      return
    }
    scan.mutate(file, {
      onSuccess: (result) =>
        result.status === 'processed' ? onResult(result.fields, null) : onResult(undefined, result.message ?? null),
    })
  }

  return (
    <div className="space-y-4">
      <p className="text-xs text-slate-600 dark:text-slate-400">
        Fotografiază tabelul nutrițional cât mai drept, cu eticheta umplând cadrul. JPEG, PNG sau WebP, cel mult 10 MB.
      </p>
      <input
        ref={input}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        capture="environment"
        className="sr-only"
        aria-label="Fotografie etichetă"
        onChange={(e) => onFile(e.target.files?.[0])}
      />
      <button
        onClick={() => input.current?.click()}
        disabled={scan.isPending}
        className="w-full flex flex-col items-center justify-center gap-2 py-8 rounded-2xl border-2 border-dashed border-emerald-400 dark:border-emerald-700 bg-emerald-50/60 dark:bg-emerald-950/20 text-emerald-800 dark:text-emerald-300 font-bold text-sm disabled:opacity-60"
      >
        {scan.isPending ? <Loader2 className="w-8 h-8 animate-spin" /> : <Camera className="w-8 h-8" />}
        {scan.isPending ? 'Se citește eticheta…' : 'Fotografiază sau alege o imagine'}
      </button>
      <FormError
        message={tooLarge ? 'Fotografia poate avea cel mult 10 MB.' : scan.error ? problemMessage(scan.error, 'Fotografia nu a putut fi trimisă.') : null}
      />
      <button onClick={onManual} className="w-full flex items-center justify-center gap-1.5 text-xs font-semibold text-slate-600 dark:text-slate-400 hover:underline">
        <ImageUp className="w-4 h-4" /> Completez manual
      </button>
    </div>
  )
}
