import createClient from 'openapi-fetch'
import { authFetch } from '@/auth/authFetch'
import type { paths } from './schema'

/**
 * Typed client for the NutriScan API: paths, parameters and bodies are checked against the OpenAPI contract
 * at compile time. Requests go to the same origin (/api), which Vite proxies to the backend in development.
 */
export const api = createClient<paths>({ baseUrl: window.location.origin, credentials: 'include', fetch: authFetch })
