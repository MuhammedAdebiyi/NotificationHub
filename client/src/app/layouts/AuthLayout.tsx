import type { ReactNode } from 'react'

export default function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-paper text-ink">
      <header className="sticky top-0 z-50 w-full">
        <div className="max-w-6xl mx-auto px-5 sm:px-8 pt-5">
          <nav className="flex items-center justify-between gap-3 bg-white/80 backdrop-blur-md border border-ink/10 rounded-full px-4 sm:px-5 py-2.5 shadow-sm">
            <a href="/" className="font-display font-extrabold text-lg tracking-tight">
              NotificationHub<span className="text-violet">.</span>
            </a>
            <div className="flex items-center gap-2">
              <a href="/login" className="px-4 py-2 rounded-full text-sm font-medium hover:bg-fog transition">Log in</a>
              <a href="/signup" className="bg-ink text-white px-4 py-2 rounded-full text-sm font-semibold hover:bg-violet transition">Sign up</a>
            </div>
          </nav>
        </div>
      </header>

      <main className="max-w-6xl mx-auto px-5 sm:px-8 py-10">
        {children}
      </main>
    </div>
  )
}