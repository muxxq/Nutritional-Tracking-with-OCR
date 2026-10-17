/**
 * Unwraps an openapi-fetch result: the data, or the ProblemDetails body as the thrown error. The HTTP status travels
 * with it, because some errors have no body (a 429 from the rate limiter).
 */
export function unwrap<T>({ data, error, response }: { data?: T; error?: unknown; response: Response }): T {
  if (data === undefined) {
    throw { ...(error as object), status: response.status }
  }
  return data
}
