/** RFC 7807 errors as the API returns them; validation errors also carry `errors` per field. */
interface Problem {
  title?: string | null
  status?: number | null
  errors?: Record<string, string[]>
}

const asProblem = (error: unknown): Problem => (error && typeof error === 'object' ? (error as Problem) : {})

/** The message to show for a failed request: the API's own title, else the given fallback. */
export function problemMessage(error: unknown, fallback: string): string {
  const problem = asProblem(error)
  if (problem.errors) {
    return Object.values(problem.errors).flat()[0] ?? fallback
  }
  return problem.title ?? fallback
}

/** Field errors keyed by camelCase field name ("Password" → "password"). */
export function fieldErrors(error: unknown): Record<string, string> {
  const errors = asProblem(error).errors ?? {}
  return Object.fromEntries(
    Object.entries(errors).map(([field, messages]) => [field.charAt(0).toLowerCase() + field.slice(1), messages[0] ?? '']),
  )
}
