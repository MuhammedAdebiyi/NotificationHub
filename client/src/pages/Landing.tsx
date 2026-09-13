import AuthLayout from '@/app/layouts/AuthLayout'

const stats = [
  { value: '99.9%', label: 'Delivery rate' },
  { value: '<2s', label: 'Queue latency' },
  { value: '5x', label: 'Retry backoff' },
  { value: '6', label: 'Official SDKs' },
]

const features = [
  {
    title: 'Queued, not blocking',
    desc: 'Every API call returns instantly. Workers handle delivery in the background — your app never waits on email.',
    rotate: '-1deg',
    accent: 'border-l-4 border-violet',
  },
  {
    title: 'Multi-provider fallback',
    desc: 'Connect Resend, SendByte, SendGrid, Brevo, or SMTP. If one fails, the next provider is tried automatically.',
    rotate: '1deg',
    accent: 'border-l-4 border-teal',
  },
  {
    title: 'Retries + Dead Letter Queue',
    desc: 'Failed sends back off automatically. After 5 attempts they land in a DLQ for manual replay.',
    rotate: '-0.5deg',
    accent: 'border-l-4 border-coral',
  },
  {
    title: 'Campaign engine',
    desc: 'Bulk email campaigns with scheduling, pause/resume, real-time progress, and import from your database.',
    rotate: '0.8deg',
    accent: 'border-l-4 border-yellow',
  },
  {
    title: 'Email templates',
    desc: 'Reusable templates with {{variable}} placeholders. Manage via API or the visual editor in the dashboard.',
    rotate: '-0.7deg',
    accent: 'border-l-4 border-violet',
  },
  {
    title: 'Idempotent sends',
    desc: 'Pass an Idempotency-Key header and we guarantee the message is sent exactly once — no duplicates.',
    rotate: '0.5deg',
    accent: 'border-l-4 border-teal',
  },
  {
    title: 'Multi-tenant by design',
    desc: 'Every org gets isolated data, API keys, and delivery stats. One platform — many clients.',
    rotate: '-0.3deg',
    accent: 'border-l-4 border-coral',
  },
  {
    title: 'Webhooks',
    desc: 'Get notified in real-time when emails are sent, delivered, failed, or bounced. Configure a URL and we POST events to your server.',
    rotate: '0.6deg',
    accent: 'border-l-4 border-yellow',
  },
  {
    title: 'Full OpenAPI spec',
    desc: 'Interactive Swagger UI, machine-readable OpenAPI JSON, and official SDKs for 6 languages.',
    rotate: '-0.4deg',
    accent: 'border-l-4 border-violet',
  },
]

const sdks = [
  { name: 'TypeScript', install: 'npm install @notificationhub/sdk', icon: 'TS' },
  { name: 'Python', install: 'pip install notificationhub', icon: 'Py' },
  { name: 'Go', install: 'go get github.com/.../sdk/go', icon: 'Go' },
  { name: 'C#', install: 'dotnet add package NotificationHub.Client', icon: 'C#' },
  { name: 'NestJS', install: 'npm install @notificationhub/nestjs', icon: 'Nj' },
  { name: 'PHP', install: 'composer require notificationhub/sdk', icon: 'Ph' },
]

const steps = [
  { step: '01', title: 'Sign up', desc: 'Create your account and org in 30 seconds.' },
  { step: '02', title: 'Get your API key', desc: 'Copy your key from Settings — shown once, store it securely.' },
  { step: '03', title: 'Send a notification', desc: 'POST to /api/v1/notifications or use an SDK.' },
  { step: '04', title: 'Track delivery', desc: 'Watch real-time status in the dashboard.' },
]

const providers = [
  { name: 'Resend', type: 'Transactional', color: '#000' },
  { name: 'SendByte', type: 'Transactional', color: '#9333ea' },
  { name: 'SendGrid', type: 'Transactional + Marketing', color: '#1A82E2' },
  { name: 'Brevo', type: 'Transactional + Marketing', color: '#0B916A' },
  { name: 'SMTP', type: 'Any', color: '#6b7280' },
]

export default function Landing() {
  return (
    <AuthLayout>
      {/* Hero */}
      <section className="text-center py-20 sm:py-28 relative">
        <p className="hand text-3xl text-violet mb-3">notifications, handled properly —</p>

        <h1 className="font-display font-extrabold text-5xl sm:text-7xl leading-[1.05] tracking-tight">
          One platform.<br />
          <span className="text-violet">Every channel.</span>
        </h1>

        <p className="mt-6 text-ink/60 max-w-2xl mx-auto text-lg leading-relaxed">
          Transactional email, SMS, and push — queued, retried, and tracked automatically.
          Built for engineering teams that can't afford to drop a message.
        </p>

        <div className="mt-10 flex flex-wrap justify-center gap-3">
          <a
            href="/signup"
            className="bg-ink text-white px-6 py-3.5 rounded-full text-sm font-semibold hover:bg-violet transition"
          >
            Start for free
          </a>
          <a
            href="https://docs.notificationhub.space/quickstart"
            className="border border-ink/20 px-6 py-3.5 rounded-full text-sm font-semibold hover:bg-fog transition"
          >
            Read the docs
          </a>
          <a
            href="https://api.notificationhub.space/docs"
            className="border border-ink/20 px-6 py-3.5 rounded-full text-sm font-semibold hover:bg-fog transition"
          >
            Swagger Explorer
          </a>
        </div>

        {/* Code snippet */}
        <div className="mt-14 max-w-lg mx-auto text-left bg-ink text-paper rounded-2xl p-6 shadow-xl" style={{ transform: 'rotate(-0.5deg)' }}>
          <div className="flex items-center gap-2 mb-4">
            <span className="w-3 h-3 rounded-full bg-coral/80" />
            <span className="w-3 h-3 rounded-full bg-yellow/80" />
            <span className="w-3 h-3 rounded-full bg-teal/80" />
            <span className="ml-2 text-paper/30 text-xs font-mono">POST /api/v1/notifications</span>
          </div>
          <pre className="text-xs font-mono leading-relaxed text-paper/80 overflow-x-auto">{`{
  "recipientEmail": "user@yourapp.com",
  "type": "transactional",
  "channel": "email",
  "payload": {
    "subject": "Welcome aboard!",
    "html": "<h1>Hi!</h1><p>Thanks for signing up.</p>"
  }
}`}</pre>
          <div className="mt-4 pt-4 border-t border-paper/10 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-teal animate-pulse" />
            <span className="text-teal text-xs font-mono">201 — queued in 12ms</span>
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="grid grid-cols-2 sm:grid-cols-4 gap-4 py-8">
        {stats.map((s, i) => (
          <div
            key={s.label}
            className="bg-fog border border-ink/10 rounded-xl p-6 text-center"
            style={{ transform: `rotate(${i % 2 === 0 ? '-0.5deg' : '0.5deg'})` }}
          >
            <p className="font-display font-extrabold text-3xl text-violet">{s.value}</p>
            <p className="text-xs text-ink/50 mt-1">{s.label}</p>
          </div>
        ))}
      </section>

      {/* Features */}
      <section id="features" className="py-16">
        <p className="hand text-2xl text-violet text-center mb-2">what you get —</p>
        <h2 className="font-display font-bold text-3xl text-center mb-10">
          Everything your notification layer needs
        </h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {features.map(f => (
            <div
              key={f.title}
              className={`bg-fog border border-ink/10 rounded-xl p-6 ${f.accent} hover:-translate-y-1 transition`}
              style={{ transform: `rotate(${f.rotate})` }}
            >
              <h3 className="font-display font-bold text-base mb-2">{f.title}</h3>
              <p className="text-sm text-ink/60 leading-relaxed">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Multi-Provider Section */}
      <section className="py-16">
        <p className="hand text-2xl text-violet text-center mb-2">never drop an email —</p>
        <h2 className="font-display font-bold text-3xl text-center mb-4">
          Multi-provider fallback
        </h2>
        <p className="text-ink/50 text-center max-w-xl mx-auto text-sm mb-10">
          Connect multiple email providers. Set a default. If it fails, the next one is tried automatically — zero code changes.
        </p>
        <div className="grid sm:grid-cols-5 gap-3">
          {providers.map(p => (
            <div key={p.name} className="bg-fog border border-ink/10 rounded-xl p-4 text-center hover:-translate-y-1 transition">
              <div className="w-10 h-10 rounded-full mx-auto mb-2 flex items-center justify-center text-white text-xs font-bold" style={{ background: p.color }}>
                {p.name.slice(0, 2)}
              </div>
              <p className="font-display font-bold text-sm">{p.name}</p>
              <p className="text-xs text-ink/40 mt-1">{p.type}</p>
            </div>
          ))}
        </div>
      </section>

      {/* SDKs Section */}
      <section id="resources" className="py-16">
        <p className="hand text-2xl text-violet text-center mb-2">ship in any language —</p>
        <h2 className="font-display font-bold text-3xl text-center mb-4">
          Official SDKs
        </h2>
        <p className="text-ink/50 text-center max-w-xl mx-auto text-sm mb-10">
          Pick your language and get started in minutes. All SDKs support send, retry, templates, campaigns, and progress tracking.
        </p>
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
          {sdks.map(s => (
            <div key={s.name} className="bg-fog border border-ink/10 rounded-xl p-4 flex items-center gap-4 hover:-translate-y-1 transition">
              <div className="w-10 h-10 rounded-lg bg-violet/10 text-violet font-display font-bold text-sm flex items-center justify-center shrink-0">
                {s.icon}
              </div>
              <div className="min-w-0">
                <p className="font-display font-bold text-sm">{s.name}</p>
                <p className="text-xs text-ink/40 font-mono truncate">{s.install}</p>
              </div>
            </div>
          ))}
        </div>
        <div className="text-center mt-8">
          <a href="https://docs.notificationhub.space/sdk/typescript" className="text-violet text-sm font-semibold hover:underline">
            Browse SDK docs →
          </a>
        </div>
      </section>

      {/* How it works */}
      <section className="py-16">
        <p className="hand text-2xl text-violet text-center mb-2">get started in minutes —</p>
        <h2 className="font-display font-bold text-3xl text-center mb-10">How it works</h2>
        <div className="grid sm:grid-cols-4 gap-6">
          {steps.map((s, i) => (
            <div key={s.step} className="relative">
              {i < steps.length - 1 && (
                <div className="hidden sm:block absolute top-5 left-full w-full h-px bg-ink/10 z-0" />
              )}
              <div className="relative z-10">
                <div className="w-10 h-10 rounded-full bg-violet/10 text-violet font-display font-bold text-sm flex items-center justify-center mb-3">
                  {s.step}
                </div>
                <h3 className="font-display font-bold text-sm mb-1">{s.title}</h3>
                <p className="text-xs text-ink/50 leading-relaxed">{s.desc}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* Resources */}
      <section className="py-16">
        <p className="hand text-2xl text-violet text-center mb-2">learn and build —</p>
        <h2 className="font-display font-bold text-3xl text-center mb-10">Resources</h2>
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <a href="https://docs.notificationhub.space/introduction" className="bg-fog border border-ink/10 rounded-xl p-6 hover:-translate-y-1 transition block">
            <p className="font-display font-bold text-sm mb-1">Documentation</p>
            <p className="text-xs text-ink/50">Guides, API reference, and SDK docs</p>
          </a>
          <a href="https://api.notificationhub.space/docs" className="bg-fog border border-ink/10 rounded-xl p-6 hover:-translate-y-1 transition block">
            <p className="font-display font-bold text-sm mb-1">Swagger Explorer</p>
            <p className="text-xs text-ink/50">Interactive API explorer — try it live</p>
          </a>
          <a href="https://github.com/MuhammedAdebiyi/NotificationHub" className="bg-fog border border-ink/10 rounded-xl p-6 hover:-translate-y-1 transition block">
            <p className="font-display font-bold text-sm mb-1">GitHub</p>
            <p className="text-xs text-ink/50">Source code, issues, contributions</p>
          </a>
          <a href="https://docs.notificationhub.space/guide/integration" className="bg-fog border border-ink/10 rounded-xl p-6 hover:-translate-y-1 transition block">
            <p className="font-display font-bold text-sm mb-1">Integration Guide</p>
            <p className="text-xs text-ink/50">Replace your email logic in 5 minutes</p>
          </a>
        </div>
      </section>

      {/* CTA */}
      <section className="py-16 text-center">
        <div className="bg-ink text-paper rounded-2xl p-12" style={{ transform: 'rotate(-0.3deg)' }}>
          <p className="hand text-2xl text-violet mb-2">ready to ship? —</p>
          <h2 className="font-display font-bold text-3xl mb-4">
            Stop building notification infrastructure.<br />Start using it.
          </h2>
          <p className="text-paper/60 max-w-md mx-auto text-sm mb-8">
            Free to start. No credit card required. Your first 10,000 notifications are on us.
          </p>
          <a
            href="/signup"
            className="inline-block bg-violet text-white px-8 py-4 rounded-full text-sm font-semibold hover:bg-violet/80 transition"
          >
            Create your account
          </a>
        </div>
      </section>
    </AuthLayout>
  )
}
