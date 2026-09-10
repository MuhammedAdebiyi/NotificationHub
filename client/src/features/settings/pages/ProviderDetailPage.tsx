import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'

interface ProviderDomain {
  id: string
  domain: string
  providerType: string
  status: string
  verifiedAt: string | null
  deliverabilityReady: boolean
}

interface ProviderHealth {
  providerType: string
  providerId: string
  isHealthy: boolean
  error: string | null
}

interface EmailProviderStatus {
  id: string
  configured: boolean
  providerType?: string
  isActive?: boolean
  isDefault?: boolean
  createdAt?: string
}

export default function ProviderDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [provider, setProvider] = useState<EmailProviderStatus | null>(null)
  const [domains, setDomains] = useState<ProviderDomain[]>([])
  const [health, setHealth] = useState<ProviderHealth | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadData()
  }, [id])

  async function loadData() {
    try {
      const [providerRes, domainRes] = await Promise.all([
        apiClient.get<{ providers: EmailProviderStatus[] }>('/api/v1/org/email-provider'),
        apiClient.get<{ domains: ProviderDomain[]; providerHealth: ProviderHealth[] }>('/api/v1/org/email-provider/domains'),
      ])

      const found = providerRes.providers.find(p => p.id === id)
      setProvider(found ?? null)

      if (found) {
        setDomains(domainRes.domains.filter(d => d.providerType === found.providerType))
        setHealth(domainRes.providerHealth.find(h => h.providerId === id) ?? null)
      }
    } catch {
      // fail silently
    } finally {
      setLoading(false)
    }
  }

  async function handleSetDefault() {
    if (!id) return
    try {
      await apiClient.put(`/api/v1/org/email-provider/${id}/default`, {})
      await loadData()
    } catch {
      // fail silently
    }
  }

  async function handleRemove() {
    if (!id) return
    try {
      await apiClient.delete(`/api/v1/org/email-provider/${id}`)
      navigate('/settings')
    } catch {
      // fail silently
    }
  }

  const providerLabels: Record<string, string> = {
    resend: 'Resend',
    sendbyte: 'SendByte',
    sendgrid: 'SendGrid',
    brevo: 'Brevo',
    smtp: 'SMTP',
  }

  if (loading) {
    return (
      <AppLayout>
        <p className="text-sm text-ink/40 text-center py-12">Loading...</p>
      </AppLayout>
    )
  }

  if (!provider) {
    return (
      <AppLayout>
        <div className="text-center py-12">
          <p className="text-sm text-ink/40">Provider not found.</p>
          <button onClick={() => navigate('/settings')} className="mt-4 text-sm text-violet hover:underline">
            Back to Settings
          </button>
        </div>
      </AppLayout>
    )
  }

  return (
    <AppLayout>
      <div className="mb-6">
        <button
          onClick={() => navigate('/settings')}
          className="text-sm text-ink/40 hover:text-violet transition mb-4 inline-flex items-center gap-1"
        >
          ← Settings
        </button>

        <div className="flex items-center gap-3 mb-1">
          <span className={`w-3 h-3 rounded-full ${health && !health.isHealthy ? 'bg-coral' : provider.isDefault ? 'bg-teal' : 'bg-ink/30'}`} />
          <h1 className="font-display font-bold text-2xl capitalize">
            {providerLabels[provider.providerType ?? ''] ?? provider.providerType}
          </h1>
          {provider.isDefault && (
            <span className="text-xs px-2 py-0.5 rounded-full bg-teal/10 text-teal font-medium">Default</span>
          )}
        </div>
        <p className="text-xs text-ink/40 mt-1">
          Connected {provider.createdAt ? new Date(provider.createdAt).toLocaleDateString() : ''}
        </p>
      </div>

      <div className="max-w-2xl space-y-6">
        {/* Health status */}
        {health && !health.isHealthy && (
          <div className="bg-coral/5 border border-coral/20 rounded-xl p-5">
            <div className="flex items-center gap-2 mb-2">
              <span className="w-2 h-2 rounded-full bg-coral" />
              <p className="text-sm font-semibold text-coral">Connection Error</p>
            </div>
            <p className="text-sm text-coral/80">{health.error}</p>
            <p className="text-xs text-ink/40 mt-3">
              Go to Settings → Email Providers → Remove this provider and re-add with a valid API key.
            </p>
          </div>
        )}

        {health && health.isHealthy && (
          <div className="bg-teal/5 border border-teal/20 rounded-xl p-5">
            <div className="flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-teal" />
              <p className="text-sm font-semibold text-teal">Connected</p>
            </div>
            <p className="text-xs text-ink/40 mt-1">API key is valid and active.</p>
          </div>
        )}

        {/* Verified Domains */}
        <div className="bg-white border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-1">Verified Domains</h2>
          <p className="text-xs text-ink/40 mb-4">
            Domains verified with this provider for sending emails.
          </p>

          {domains.length === 0 ? (
            <div className="bg-fog/50 border border-ink/10 rounded-lg p-4">
              <p className="text-sm text-ink/60 font-medium mb-2">No domains verified yet</p>
              <ol className="text-xs text-ink/50 space-y-1.5 list-decimal list-inside">
                <li>Go to {providerLabels[provider.providerType ?? ''] ?? provider.providerType} dashboard → Domains</li>
                <li>Add your domain (e.g. <code className="bg-white px-1 py-0.5 rounded border border-ink/10">example.com</code>)</li>
                <li>Add the DNS records (TXT, CNAME, or MX) to your domain registrar</li>
                <li>Wait for verification (usually 5-30 minutes)</li>
                <li>Once verified, you can use this domain as the sender address</li>
              </ol>
            </div>
          ) : (
            <div className="space-y-2">
              {domains.map(d => (
                <div key={d.id} className="flex items-center justify-between py-3 px-4 bg-fog/50 rounded-lg border border-ink/5">
                  <div className="flex items-center gap-3">
                    <span className={`w-2 h-2 rounded-full ${d.status === 'verified' ? 'bg-teal' : 'bg-amber'}`} />
                    <span className="text-sm font-mono">{d.domain}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className={`text-xs px-2 py-0.5 rounded-full ${
                      d.status === 'verified'
                        ? 'bg-teal/10 text-teal'
                        : 'bg-amber/10 text-amber'
                    }`}>
                      {d.status === 'verified' ? 'Active' : 'Pending'}
                    </span>
                    {d.deliverabilityReady && (
                      <span className="text-xs px-2 py-0.5 rounded-full bg-violet/10 text-violet">
                        Ready
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Actions */}
        <div className="flex gap-3">
          {!provider.isDefault && (
            <button
              onClick={handleSetDefault}
              className="px-4 py-2.5 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition"
            >
              Set as Default
            </button>
          )}
          <button
            onClick={handleRemove}
            className="px-4 py-2.5 border border-coral/30 text-coral rounded-lg text-sm font-medium hover:bg-red-50 transition"
          >
            Remove Provider
          </button>
        </div>
      </div>
    </AppLayout>
  )
}
