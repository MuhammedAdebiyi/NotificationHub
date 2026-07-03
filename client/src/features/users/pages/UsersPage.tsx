import { useState, useEffect } from 'react'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface User {
  id: string
  fullName: string
  email: string
  isEmailVerified: boolean
  createdAt: string
  notificationCount: number
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [totalCount, setTotalCount] = useState(0)

  useEffect(() => {
    apiClient.get<{ items: User[]; totalCount: number }>('/api/v1/users')
      .then(res => {
        setUsers(res.items)
        setTotalCount(res.totalCount)
      })
      .catch(() => {})
      .finally(() => setIsLoading(false))
  }, [])

  return (
    <AppLayout>
      <div className="mb-6">
        <h1 className="font-display font-bold text-3xl mb-1">Users</h1>
        <p className="text-ink/50 text-sm">
          {totalCount} recipient{totalCount !== 1 ? 's' : ''} in the system
        </p>
      </div>

      {isLoading ? (
        <p className="text-ink/50 text-sm">Loading...</p>
      ) : users.length === 0 ? (
        <div className="text-center py-16 text-ink/40">
          <p className="text-lg font-medium mb-1">No users yet</p>
          <p className="text-sm">Users appear here once notifications are sent to them.</p>
        </div>
      ) : (
        <div className="bg-white border border-ink/10 rounded-xl overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-fog/40 border-b border-ink/10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">Name</th>
                <th className="text-left px-4 py-3 font-medium">Email</th>
                <th className="text-left px-4 py-3 font-medium">Verified</th>
                <th className="text-left px-4 py-3 font-medium">Notifications</th>
                <th className="text-left px-4 py-3 font-medium">Joined</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-ink/5">
              {users.map(u => (
                <tr key={u.id} className="hover:bg-fog/20 transition">
                  <td className="px-4 py-3 font-medium">{u.fullName}</td>
                  <td className="px-4 py-3 text-ink/60">{u.email}</td>
                  <td className="px-4 py-3">
                    {u.isEmailVerified ? (
                      <span className="text-xs px-2 py-0.5 bg-teal/10 text-teal rounded-full">Verified</span>
                    ) : (
                      <span className="text-xs px-2 py-0.5 bg-yellow/10 text-yellow-700 rounded-full">Unverified</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-ink/60">{u.notificationCount}</td>
                  <td className="px-4 py-3 text-ink/40">
                    {new Date(u.createdAt).toLocaleDateString()}
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