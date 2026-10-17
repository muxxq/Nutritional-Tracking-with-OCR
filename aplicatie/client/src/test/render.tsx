import type { ReactElement } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { vi } from 'vitest'

/** Renders a page at `path` with a fresh query cache; `routes` adds the pages it may navigate to. */
export function renderPage(path: string, page: ReactElement, routes: Record<string, string> = {}) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path={path} element={page} />
          {Object.entries(routes).map(([to, text]) => <Route key={to} path={to} element={<p>{text}</p>} />)}
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  )
  return queryClient
}

type Handler = (request: Request) => Response | Promise<Response>

/**
 * A fake API: "METHOD /path" → handler. Unmatched requests fail the test loudly instead of hanging.
 * Returns the fetch mock, whose calls hold the Requests the page sent.
 */
export function fakeApi(handlers: Record<string, Handler>) {
  const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
    const request = input as Request
    const key = `${request.method} ${new URL(request.url).pathname}`
    const handler = handlers[key]
    if (!handler) {
      throw new Error(`Unexpected request: ${key}`)
    }
    return handler(request)
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

export const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
