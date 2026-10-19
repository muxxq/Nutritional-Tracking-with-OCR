import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'
import { useSession } from '@/auth/session'

export type ProfileDto = Schemas['ProfileDto']
export type UpdateProfileRequest = Schemas['UpdateProfileRequest']
export type UpdateTargetsRequest = Schemas['UpdateTargetsRequest']

export const profileKey = ['profile'] as const

export function useProfile() {
  return useQuery({
    queryKey: profileKey,
    queryFn: async () => unwrap(await api.GET('/api/profile')),
  })
}

function useProfileMutation<TBody>(send: (body: TBody) => Promise<ProfileDto>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: send,
    onSuccess: (profile) => {
      queryClient.setQueryData(profileKey, profile)
      // Targets feed the diary, plan and statistics screens: their cached copies are now stale.
      void queryClient.invalidateQueries({ predicate: (q) => q.queryKey[0] !== profileKey[0] })
      const user = useSession.getState().user
      if (user) {
        useSession.getState().updateUser({ ...user, firstName: profile.firstName, lastName: profile.lastName, isProfileComplete: profile.isProfileComplete })
      }
    },
  })
}

export const useUpdateProfile = () =>
  useProfileMutation(async (body: UpdateProfileRequest) => unwrap(await api.PUT('/api/profile', { body })))

export const useUpdateTargets = () =>
  useProfileMutation(async (body: UpdateTargetsRequest) => unwrap(await api.PUT('/api/profile/targets', { body })))
