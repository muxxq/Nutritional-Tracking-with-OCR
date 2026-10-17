import type { components } from './schema'

/** DTOs of the NutriScan API, generated from aplicatie/backend/openapi.json (npm run api:types). */
export type Schemas = components['schemas']

export type AuthResponse = Schemas['AuthResponse']
export type UserDto = Schemas['UserDto']
