import type { ReactNode } from 'react'
import { NavLink } from 'react-router-dom'

const navItems = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/notifications', label: 'Notifications' },
  { to: '/templates', label: 'Templates' },
  { to: '/campaigns', label: 'Campaigns' },
  { to: '/analytics', label: 'Analytics' },
  { to: '/users', label: 'Users' },
  { to: '/settings', label: 'Settings' },
]

export default function AppLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-paper text-ink flex">
      <aside className="w-64 shrink-0 border-r border-ink/10 bg-fog/40 flex flex-col">
        <div className="px-6 py-6">
          <a href="/dashboard" className="font-display font-extrabold text-lg tracking-tight">
            NotificationHub<span className="text-violet">.</span>
          </a>
        </div>

        <nav className="flex-1 px-3 space-y-1">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `block px-4 py-2.5 rounded-lg text-sm font-medium transition ${
                  isActive
                    ? 'bg-ink text-white'
                    : 'text-ink/70 hover:bg-fog hover:text-ink'
                }`
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="px-6 py-5 border-t border-ink/10">
          <span className="text-xs font-medium px-3 py-1.5 rounded-full bg-teal/10 text-teal">● Live</span>
        </div>
      </aside>

      <main className="flex-1 px-6 sm:px-10 py-8 overflow-y-auto">
        {children}
      </main>
    </div>
  )
}