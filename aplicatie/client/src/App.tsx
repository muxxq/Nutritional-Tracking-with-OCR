import { Navigate, Route, Routes } from 'react-router'
import { RedirectIfSignedIn, RequireAuth } from '@/auth/RequireAuth'
import { useSessionBootstrap } from '@/auth/useSessionBootstrap'
import { AppLayout } from '@/layout/AppLayout'
import { PlannerRoute, ShoppingRoute } from '@/layout/legacyRoutes'
import { RecipesPage } from '@/features/recipes/RecipesPage'
import { DiaryPage } from '@/features/diary/DiaryPage'
import { ProfilePage } from '@/features/profile/ProfilePage'
import { LoginPage } from '@/pages/LoginPage'
import { RegisterPage } from '@/pages/RegisterPage'

export function App() {
  useSessionBootstrap()

  return (
    <Routes>
      <Route element={<RedirectIfSignedIn />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/diary" replace />} />
          <Route path="/diary" element={<DiaryPage />} />
          <Route path="/planner" element={<PlannerRoute />} />
          <Route path="/recipes" element={<RecipesPage />} />
          <Route path="/shopping" element={<ShoppingRoute />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/onboarding" element={<ProfilePage onboarding />} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
