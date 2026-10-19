import { Loader2 } from 'lucide-react'
import { Link, useNavigate } from 'react-router'
import { problemMessage } from '@/api/problem'
import { Card, FormError } from '@/components/ui/form'
import { ProfileForm } from './ProfileForm'
import { TargetsPanel } from './TargetsPanel'
import { useProfile } from './profileApi'

/**
 * FR-02. As onboarding (right after registering) it asks only for the profile and then opens the diary;
 * as /profile it also shows the targets and their manual overrides.
 */
export function ProfilePage({ onboarding = false }: { onboarding?: boolean }) {
  const navigate = useNavigate()
  const profile = useProfile()

  if (profile.isPending) {
    return (
      <div className="flex justify-center py-20" role="status" aria-label="Se încarcă profilul">
        <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
      </div>
    )
  }

  if (profile.isError) {
    return <FormError message={problemMessage(profile.error, 'Profilul nu a putut fi încărcat.')} />
  }

  if (onboarding) {
    return (
      <div className="max-w-xl mx-auto space-y-4">
        <Card title="Hai să-ți calculăm țintele" subtitle="Din datele de mai jos calculăm necesarul caloric și macronutrienții zilnici.">
          <ProfileForm profile={profile.data} submitLabel="Calculează țintele" onSaved={() => navigate('/diary', { replace: true })} />
        </Card>
        <p className="text-center text-sm">
          <Link to="/diary" className="font-semibold text-slate-600 dark:text-slate-400 hover:underline">Mai târziu</Link>
        </p>
      </div>
    )
  }

  return (
    <div className="max-w-3xl mx-auto space-y-5">
      <Card title="Ținte zilnice" subtitle="Calculate din profil; le poți ajusta manual.">
        {/* Remount after a save, so the override fields show what the server now stores. */}
        <TargetsPanel key={JSON.stringify(profile.data.targets)} profile={profile.data} />
      </Card>
      <Card title="Profil" subtitle={profile.data.email}>
        <ProfileForm key={JSON.stringify(profile.data)} profile={profile.data} submitLabel="Salvează profilul" />
      </Card>
    </div>
  )
}
