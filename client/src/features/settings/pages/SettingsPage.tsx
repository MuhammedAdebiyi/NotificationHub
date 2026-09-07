import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'
import { apiClient } from '@/shared/services/apiClient'
import { authService } from '@/shared/services/auth.service'
import DataSourcesSection from '@/features/settings/components/DataSourcesSection'

interface ApiKey {
  id: string
  name: string
  keyPrefix: string
  isActive: boolean
  createdAt: string
  lastUsedAt: string | null
}

interface OrgInfo {
  id: string
  name: string
  slug: string
  plan: string
  fromName: string
  fromEmail: string
  createdAt: string
}

interface EmailProviderStatus {
  configured: boolean
  providerType?: string
  senderEmail?: string
  isActive?: boolean
  createdAt?: string
}

const SENDER_EMAIL_OPTIONS = [
  'notifications@mail.notificationhub.space',
]

function PermissionWall() {
  const navigate = useNavigate()
  return (
    <AppLayout>
      <div className="flex flex-col items-center justify-center py-24 text-center">
        <div className="w-16 h-16 rounded-full bg-coral/10 flex items-center justify-center mb-6">
          <span className="text-coral text-2xl">⊘</span>
        </div>
        <p className="hand text-xl text-coral mb-2">no access —</p>
        <h2 className="font-display font-bold text-2xl mb-3">
          You don't have permission
        </h2>
        <p className="text-sm text-ink/50 max-w-sm mb-8">
          Settings management is restricted to owners and admins.
          Contact your organization owner if you need access.
        </p>
        <button
          onClick={() => navigate('/dashboard')}
          className="px-5 py-2.5 bg-ink text-white rounded-xl text-sm font-medium hover:bg-violet transition"
        >
          Back to dashboard
        </button>
      </div>
    </AppLayout>
  )
}

function RevokeConfirmModal({
  keyName,
  onConfirm,
  onCancel,
}: {
  keyName: string
  onConfirm: () => void
  onCancel: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={onCancel} />
      <div className="relative bg-white rounded-2xl p-8 max-w-sm w-full mx-4 shadow-2xl">
        <div className="w-12 h-12 bg-coral/10 rounded-full flex items-center justify-center mb-4">
          <span className="text-coral text-xl">⊘</span>
        </div>
        <h3 className="font-display font-bold text-lg mb-1">Revoke API key?</h3>
        <p className="text-sm text-ink/60 mb-6">
          <span className="font-medium text-ink">"{keyName}"</span> will stop working immediately.
          Any service using this key will lose access. This cannot be undone.
        </p>
        <div className="flex gap-3">
          <button
            onClick={onConfirm}
            className="flex-1 px-4 py-2.5 bg-coral text-white rounded-lg text-sm font-medium hover:bg-coral/80 transition"
          >
            Revoke
          </button>
          <button
            onClick={onCancel}
            className="flex-1 px-4 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
          >
            Cancel
          </button>
        </div>
      </div>
    </div>
  )
}

export default function SettingsPage() {
  const currentUser = authService.getUser()
  const currentRole = currentUser?.role ?? 'member'
  const canManage = currentRole === 'owner' || currentRole === 'admin'

  const [keys, setKeys] = useState<ApiKey[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [newKeyName, setNewKeyName] = useState('')
  const [creating, setCreating] = useState(false)
  const [newKeyValue, setNewKeyValue] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [revokeTarget, setRevokeTarget] = useState<ApiKey | null>(null)

  const [orgInfo, setOrgInfo] = useState<OrgInfo | null>(null)
  const [orgLoading, setOrgLoading] = useState(true)
  const [fromName, setFromName] = useState('')
  const [savingFromName, setSavingFromName] = useState(false)
  const [fromNameSaved, setFromNameSaved] = useState(false)
  const [fromNameError, setFromNameError] = useState<string | null>(null)

  const [fromEmail, setFromEmail] = useState('')
  const [savingFromEmail, setSavingFromEmail] = useState(false)
  const [fromEmailSaved, setFromEmailSaved] = useState(false)
  const [fromEmailError, setFromEmailError] = useState<string | null>(null)

  // Email provider state
  const [emailProvider, setEmailProvider] = useState<EmailProviderStatus | null>(null)
  const [emailProviderLoading, setEmailProviderLoading] = useState(true)
  const [showProviderForm, setShowProviderForm] = useState(false)
  const [providerApiKey, setProviderApiKey] = useState('')
  const [providerType, setProviderType] = useState('resend')
  const [providerSaving, setProviderSaving] = useState(false)
  const [providerError, setProviderError] = useState<string | null>(null)
  const [providerSaved, setProviderSaved] = useState(false)
  const [showRemoveProvider, setShowRemoveProvider] = useState(false)

  // SMTP-specific fields
  const [smtpHost, setSmtpHost] = useState('')
  const [smtpPort, setSmtpPort] = useState('587')
  const [smtpUsername, setSmtpUsername] = useState('')
  const [smtpPassword, setSmtpPassword] = useState('')

  if (!canManage) return <PermissionWall />

  async function load() {
    try {
      const res = await apiClient.get<{ items: ApiKey[] }>('/api/v1/org/api-keys')
      setKeys(res.items)
    } catch {
      // fail silently
    } finally {
      setIsLoading(false)
    }
  }

  async function loadOrgInfo() {
    try {
      const res = await apiClient.get<OrgInfo>('/api/v1/org/info')
      setOrgInfo(res)
      setFromName(res.fromName ?? '')
      setFromEmail(res.fromEmail ?? 'notifications@mail.notificationhub.space')
    } catch {
      // fail silently
    } finally {
      setOrgLoading(false)
    }
  }

  async function loadEmailProvider() {
    try {
      const res = await apiClient.get<EmailProviderStatus>('/api/v1/org/email-provider')
      setEmailProvider(res)
    } catch {
      // fail silently
    } finally {
      setEmailProviderLoading(false)
    }
  }

  useEffect(() => {
    load()
    loadOrgInfo()
    loadEmailProvider()
  }, [])

  async function handleCreate() {
    if (!newKeyName.trim()) return
    setCreating(true)
    setError(null)
    try {
      const res = await apiClient.post<{ key: string; id: string; name: string; keyPrefix: string; createdAt: string }>(
        '/api/v1/org/api-keys',
        { name: newKeyName }
      )
      setNewKeyValue(res.key)
      setNewKeyName('')
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create key.')
    } finally {
      setCreating(false)
    }
  }

  async function handleRevoke(key: ApiKey) {
    setRevokeTarget(key)
  }

  async function confirmRevoke() {
    if (!revokeTarget) return
    try {
      await apiClient.delete(`/api/v1/org/api-keys/${revokeTarget.id}`)
      setRevokeTarget(null)
      await load()
    } catch {
      setRevokeTarget(null)
    }
  }

  async function handleSaveFromName() {
    if (!fromName.trim()) return
    setSavingFromName(true)
    setFromNameSaved(false)
    setFromNameError(null)
    try {
      await apiClient.put<{ updated: boolean; fromName: string }>('/api/v1/org/info', { fromName: fromName.trim() })
      setFromNameSaved(true)
      setOrgInfo(prev => prev ? { ...prev, fromName: fromName.trim() } : prev)
    } catch (err) {
      setFromNameError(err instanceof Error ? err.message : 'Failed to save sender name.')
    } finally {
      setSavingFromName(false)
    }
  }

  async function handleSaveFromEmail() {
    if (!fromEmail.trim()) return
    setSavingFromEmail(true)
    setFromEmailSaved(false)
    setFromEmailError(null)
    try {
      await apiClient.put<{ updated: boolean; fromEmail: string }>('/api/v1/org/info', {
        fromName: fromName.trim() || orgInfo?.fromName || '',
        fromEmail: fromEmail.trim(),
      })
      setFromEmailSaved(true)
      setOrgInfo(prev => prev ? { ...prev, fromEmail: fromEmail.trim() } : prev)
    } catch (err) {
      setFromEmailError(err instanceof Error ? err.message : 'Failed to save sender email.')
    } finally {
      setSavingFromEmail(false)
    }
  }

  async function handleSaveProvider() {
    let apiKey = providerApiKey.trim()

    // Build SMTP credentials string
    if (providerType === 'smtp') {
      if (!smtpHost.trim() || !smtpPort.trim() || !smtpUsername.trim() || !smtpPassword.trim()) {
        setProviderError('All SMTP fields are required.')
        return
      }
      apiKey = `${smtpHost.trim()}|${smtpPort.trim()}|${smtpUsername.trim()}|${smtpPassword.trim()}`
    }

    if (!apiKey) return
    setProviderSaving(true)
    setProviderError(null)
    setProviderSaved(false)
    try {
      await apiClient.post('/api/v1/org/email-provider', {
        providerType,
        apiKey,
      })
      setProviderSaved(true)
      setShowProviderForm(false)
      setProviderApiKey('')
      setSmtpHost('')
      setSmtpPort('587')
      setSmtpUsername('')
      setSmtpPassword('')
      await loadEmailProvider()
    } catch (err) {
      setProviderError(err instanceof Error ? err.message : 'Failed to save provider.')
    } finally {
      setProviderSaving(false)
    }
  }

  async function handleRemoveProvider() {
    try {
      await apiClient.delete('/api/v1/org/email-provider')
      setEmailProvider(null)
      setShowRemoveProvider(false)
    } catch {
      // fail silently
    }
  }

  return (
    <AppLayout>
      {revokeTarget && (
        <RevokeConfirmModal
          keyName={revokeTarget.name}
          onConfirm={confirmRevoke}
          onCancel={() => setRevokeTarget(null)}
        />
      )}

      {showRemoveProvider && (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
          <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={() => setShowRemoveProvider(false)} />
          <div className="relative bg-white rounded-2xl p-8 max-w-sm w-full mx-4 shadow-2xl">
            <div className="w-12 h-12 bg-coral/10 rounded-full flex items-center justify-center mb-4">
              <span className="text-coral text-xl">✕</span>
            </div>
            <h3 className="font-display font-bold text-lg mb-1">Remove email provider?</h3>
            <p className="text-sm text-ink/60 mb-6">
              Your campaigns will revert to sending from the platform default email.
            </p>
            <div className="flex gap-3">
              <button
                onClick={handleRemoveProvider}
                className="flex-1 px-4 py-2.5 bg-coral text-white rounded-lg text-sm font-medium hover:bg-coral/80 transition"
              >
                Remove
              </button>
              <button
                onClick={() => setShowRemoveProvider(false)}
                className="flex-1 px-4 py-2.5 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="mb-8">
        <p className="hand text-xl text-violet mb-1">configuration —</p>
        <h1 className="font-display font-bold text-3xl">Settings</h1>
      </div>

      <div className="max-w-2xl space-y-8">
        {/* New key revealed — copy once banner */}
        {newKeyValue && (
          <div className="bg-teal/10 border border-teal/30 rounded-xl p-5">
            <p className="text-sm font-semibold text-teal mb-1">
              ✓ API key created
            </p>
            <p className="text-xs text-teal/70 mb-3">
              Copy it now — this is the only time it will be shown.
            </p>
            <code className="block bg-white border border-teal/20 rounded-lg px-4 py-3 text-sm font-mono break-all select-all">
              {newKeyValue}
            </code>
            <div className="flex gap-2 mt-3">
              <button
                onClick={() => navigator.clipboard.writeText(newKeyValue)}
                className="text-xs px-3 py-1.5 bg-teal text-white rounded-lg hover:bg-teal/80 transition"
              >
                Copy
              </button>
              <button
                onClick={() => setNewKeyValue(null)}
                className="text-xs px-3 py-1.5 border border-teal/30 text-teal rounded-lg hover:bg-teal/10 transition"
              >
                Dismiss
              </button>
            </div>
          </div>
        )}

        {/* Email Provider section */}
        <div className="bg-white border border-ink/10 rounded-xl p-6">
          <div className="flex items-center justify-between mb-1">
            <h2 className="font-display font-bold text-lg">Email Provider</h2>
          </div>
          <p className="text-xs text-ink/40 mb-6">
            Connect your own email provider to send campaigns from your domain.
            Without this, emails send from the platform default.
          </p>

          {emailProviderLoading ? (
            <p className="text-sm text-ink/40 text-center py-6">Loading...</p>
          ) : emailProvider?.configured && !showProviderForm ? (
            <div className="space-y-4">
              <div className="bg-teal/5 border border-teal/20 rounded-lg p-4">
                <div className="flex items-center justify-between">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <span className="w-2 h-2 rounded-full bg-teal" />
                      <span className="text-sm font-medium capitalize">{emailProvider.providerType} connected</span>
                    </div>
                    <p className="text-xs text-ink/40">
                      Campaigns will send through your {emailProvider.providerType} account.
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => setShowProviderForm(true)}
                      className="text-xs px-3 py-1.5 border border-ink/20 rounded-lg hover:bg-fog transition"
                    >
                      Update key
                    </button>
                    <button
                      onClick={() => setShowRemoveProvider(true)}
                      className="text-xs px-3 py-1.5 border border-coral/30 text-coral rounded-lg hover:bg-red-50 transition"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              </div>
            </div>
          ) : showProviderForm ? (
            <div className="space-y-4">
              {providerError && (
                <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl">{providerError}</div>
              )}
              {providerSaved && (
                <div className="bg-teal/10 text-teal text-sm px-4 py-3 rounded-xl">✓ Provider connected</div>
              )}
              <div>
                <label className="block text-sm font-medium mb-1">Provider</label>
                <select
                  value={providerType}
                  onChange={e => { setProviderType(e.target.value); setProviderApiKey(''); setProviderError(null) }}
                  className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                >
                  <option value="resend">Resend</option>
                  <option value="sendgrid">SendGrid</option>
                  <option value="brevo">Brevo</option>
                  <option value="smtp">SMTP (any provider)</option>
                </select>
              </div>

              {providerType === 'smtp' ? (
                <div className="space-y-3">
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="block text-sm font-medium mb-1">SMTP Host</label>
                      <input
                        type="text"
                        placeholder="smtp.sendbyte.africa"
                        value={smtpHost}
                        onChange={e => setSmtpHost(e.target.value)}
                        className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium mb-1">Port</label>
                      <input
                        type="text"
                        placeholder="587"
                        value={smtpPort}
                        onChange={e => setSmtpPort(e.target.value)}
                        className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
                      />
                    </div>
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">Username</label>
                    <input
                      type="text"
                      placeholder="your-smtp-username"
                      value={smtpUsername}
                      onChange={e => setSmtpUsername(e.target.value)}
                      className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium mb-1">Password</label>
                    <input
                      type="password"
                      placeholder="your-smtp-password"
                      value={smtpPassword}
                      onChange={e => setSmtpPassword(e.target.value)}
                      className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
                    />
                  </div>
                </div>
              ) : (
                <div>
                  <label className="block text-sm font-medium mb-1">API Key</label>
                  <input
                    type="password"
                    placeholder={providerType === 'resend' ? 're_...' : providerType === 'sendgrid' ? 'SG...' : 'xkeysib-...'}
                    value={providerApiKey}
                    onChange={e => setProviderApiKey(e.target.value)}
                    className="w-full border border-ink/20 rounded-lg px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-violet"
                  />
                </div>
              )}

              <p className="text-xs text-ink/40">
                Your credentials are encrypted and stored securely. Only used to send emails on your behalf.
              </p>
              <div className="flex gap-3">
                <button
                  onClick={handleSaveProvider}
                  disabled={providerSaving || !providerApiKey.trim()}
                  className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition disabled:opacity-50"
                >
                  {providerSaving ? 'Connecting...' : 'Connect'}
                </button>
                <button
                  onClick={() => { setShowProviderForm(false); setProviderError(null); setProviderSaved(false); setProviderApiKey('') }}
                  className="px-4 py-2 border border-ink/20 rounded-lg text-sm font-medium hover:bg-fog transition"
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <div>
              <div className="bg-fog/50 border border-ink/10 rounded-lg p-4 mb-4">
                <p className="text-sm text-ink/60">
                  No email provider connected. Emails send from the platform default
                  (<span className="font-mono text-xs">notifications@mail.notificationhub.space</span>).
                </p>
              </div>
              <button
                onClick={() => setShowProviderForm(true)}
                className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium hover:bg-violet transition"
              >
                + Connect Provider
              </button>
            </div>
          )}
        </div>

        {/* API Keys section */}
        <div className="bg-white border border-ink/10 rounded-xl p-6">
          <div className="flex items-center justify-between mb-1">
            <h2 className="font-display font-bold text-lg">API Keys</h2>
          </div>
          <p className="text-xs text-ink/40 mb-6">
            Use these keys to authenticate requests from your backend services.
            Keys are shown once — store them securely.
          </p>

          {/* Create */}
          <div className="flex gap-3 mb-6">
            <input
              className="flex-1 border border-ink/20 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
              placeholder="Key name — e.g. CourseVault Production"
              value={newKeyName}
              onChange={e => setNewKeyName(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleCreate()}
            />
            <button
              onClick={handleCreate}
              disabled={creating || !newKeyName.trim()}
              className="px-4 py-2 bg-ink text-white rounded-lg text-sm font-medium disabled:opacity-50 hover:bg-violet transition"
            >
              {creating ? 'Creating...' : '+ Create'}
            </button>
          </div>

          {error && (
            <div className="bg-coral/10 text-coral text-sm px-4 py-3 rounded-xl mb-4">
              {error}
            </div>
          )}

          {/* Key list */}
          {isLoading ? (
            <p className="text-sm text-ink/40 text-center py-6">Loading...</p>
          ) : keys.length === 0 ? (
            <div className="text-center py-8 text-ink/40">
              <p className="text-sm">No API keys yet.</p>
              <p className="text-xs mt-1">Create one above to start authenticating requests.</p>
            </div>
          ) : (
            <div className="divide-y divide-ink/5">
              {keys.map(k => (
                <div key={k.id} className="flex items-center justify-between py-3.5">
                  <div>
                    <p className="text-sm font-medium">{k.name}</p>
                    <p className="text-xs text-ink/40 font-mono mt-0.5">
                      {k.keyPrefix}••••••••••••••••••••••
                    </p>
                    <p className="text-xs text-ink/30 mt-0.5">
                      Created {new Date(k.createdAt).toLocaleDateString()}
                      {k.lastUsedAt && ` · Last used ${new Date(k.lastUsedAt).toLocaleDateString()}`}
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-xs px-2 py-0.5 rounded-full bg-teal/10 text-teal font-medium">
                      Active
                    </span>
                    <button
                      onClick={() => handleRevoke(k)}
                      className="text-xs px-3 py-1.5 border border-coral/30 text-coral rounded-lg hover:bg-red-50 transition"
                    >
                      Revoke
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Data Sources section */}
        <DataSourcesSection />

        {/* Org info */}
        <div className="bg-white border border-ink/10 rounded-xl p-6">
          <h2 className="font-display font-bold text-lg mb-1">Organization</h2>
          <p className="text-xs text-ink/40 mb-4">Your organization details.</p>

          {orgLoading ? (
            <p className="text-sm text-ink/40 py-4 text-center">Loading...</p>
          ) : (
            <div className="space-y-3 text-sm">
              <div className="flex justify-between py-2 border-b border-ink/5">
                <span className="text-ink/50">Plan</span>
                <span className="font-medium capitalize">{orgInfo?.plan ?? '—'}</span>
              </div>
              <div className="flex justify-between py-2 border-b border-ink/5">
                <span className="text-ink/50">Your role</span>
                <span className="font-medium capitalize">{currentRole}</span>
              </div>

              <div className="py-3">
                <div className="flex justify-between items-center mb-2">
                  <span className="text-ink/50">Sender name</span>
                  <span className="text-xs text-ink/30 font-mono">@notificationhub.space</span>
                </div>
                <p className="text-xs text-ink/40 mb-2">
                  This is the name recipients see when your campaigns land in their inbox —
                  e.g. "{fromName || 'NotificationHub'} &lt;notifications@mail.notificationhub.space&gt;".
                </p>
                <div className="flex gap-2">
                  <input
                    className="flex-1 border border-ink/20 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-violet"
                    placeholder="e.g. NotificationHub"
                    value={fromName}
                    onChange={e => { setFromName(e.target.value); setFromNameSaved(false); setFromNameError(null) }}
                    maxLength={100}
                  />
                  <button
                    onClick={handleSaveFromName}
                    disabled={savingFromName || !fromName.trim() || fromName.trim() === orgInfo?.fromName}
                    className="text-xs px-3 py-1.5 bg-ink text-white rounded-lg hover:bg-violet transition disabled:opacity-50 whitespace-nowrap"
                  >
                    {savingFromName ? 'Saving...' : 'Save'}
                  </button>
                </div>
                {fromNameError && (
                  <p className="text-xs text-coral mt-1">{fromNameError}</p>
                )}
                {fromNameSaved && !fromNameError && (
                  <p className="text-xs text-teal mt-1">✓ Saved</p>
                )}
              </div>

              <div className="py-3">
                <div className="flex justify-between items-center mb-2">
                  <span className="text-ink/50">Sender email</span>
                  <span className="text-xs text-ink/30 font-mono">@notificationhub.space</span>
                </div>
                <p className="text-xs text-ink/40 mb-2">
                  The address your campaigns are sent from. Pick the one recipients should see
                  and reply to.
                </p>
                <div className="flex gap-2">
                  <select
                    className="flex-1 border border-ink/20 rounded-lg px-3 py-1.5 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-violet"
                    value={fromEmail}
                    onChange={e => { setFromEmail(e.target.value); setFromEmailSaved(false); setFromEmailError(null) }}
                  >
                    {!SENDER_EMAIL_OPTIONS.includes(fromEmail) && fromEmail && (
                      <option value={fromEmail}>{fromEmail}</option>
                    )}
                    {SENDER_EMAIL_OPTIONS.map(opt => (
                      <option key={opt} value={opt}>{opt}</option>
                    ))}
                  </select>
                  <button
                    onClick={handleSaveFromEmail}
                    disabled={savingFromEmail || !fromEmail.trim() || fromEmail.trim() === orgInfo?.fromEmail}
                    className="text-xs px-3 py-1.5 bg-ink text-white rounded-lg hover:bg-violet transition disabled:opacity-50 whitespace-nowrap"
                  >
                    {savingFromEmail ? 'Saving...' : 'Save'}
                  </button>
                </div>
                {fromEmailError && (
                  <p className="text-xs text-coral mt-1">{fromEmailError}</p>
                )}
                {fromEmailSaved && !fromEmailError && (
                  <p className="text-xs text-teal mt-1">✓ Saved</p>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </AppLayout>
  )
}
