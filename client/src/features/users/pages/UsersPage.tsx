import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface Member {
  id: string
  fullName: string
  email: string
  role: string
  joinedAt: string
}

export default function UsersPage() {
  const [members, setMembers] = useState<Member[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    apiClient.get<Member[]>('/api/v1/org/members')
      .then(setMembers)
      .catch(() => {})
      .finally(() => setIsLoading(false))
  }, [])

  const roleStyle: Record<string, string> = {
    owner: 'bg-violet/10 text-violet',
    admin: 'bg-teal/10 text-teal',
    member: 'bg-fog text-ink/60',
  }

  return (
    <AppLayout>
      <div className="mb-8 flex items-center justify-between">
        <div>
          <p className="hand text-xl text-violet mb-1">your team —</p>
          <h1 className="font-display font-bold text-3xl">Team</h1>
        </div>
        <button className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-ink/80 transition">
          + Invite Member
        </button>
      </div>

      {isLoading ? (
        <p className="text-sm text-ink/40">Loading...</p>
      ) : members.length === 0 ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">Just you for now</p>
          <p className="text-sm">Invite teammates to collaborate on this organization.</p>
        </div>
      ) : (
        <div className="bg-fog border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-fog/40 border-b border-ink/10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">Name</th>
                <th className="text-left px-4 py-3 font-medium">Email</th>
                <th className="text-left px-4 py-3 font-medium">Role</th>
                <th className="text-left px-4 py-3 font-medium">Joined</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {members.map(m => (
                <tr key={m.id} className="hover:bg-fog/20 transition">
                  <td className="px-4 py-3 font-medium">{m.fullName}</td>
                  <td className="px-4 py-3 text-ink/60">{m.email}</td>
                  <td className="px-4 py-3">
                    <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${roleStyle[m.role] ?? 'bg-fog text-ink/60'}`}>
                      {m.role}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-ink/40">
                    {new Date(m.joinedAt).toLocaleDateString()}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </AppLayout>
  )
}