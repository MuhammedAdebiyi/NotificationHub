import AuthLayout from '@/app/layouts/AuthLayout'

const stats = [
  { value: '99.9%', label: 'Delivery rate' },
  { value: '<2s', label: 'Queue latency' },
  { value: '5x', label: 'Retry backoff' },
]

const features = [
  {
    title: 'Queued, not blocking',
    desc: 'Every API call returns instantly. Workers handle delivery in the background — your app never waits on email.',
    rotate: '-1deg',
    accent: 'border-l-4 border-violet',
  },
  {
    title: 'Retries + Dead Letter Queue',
    desc: 'Failed sends back off automatically. After 5 attempts they land in a DLQ for manual replay.',
    rotate: '1deg',
    accent: 'border-l-4 border-teal',
  },
  {
    title: 'Multi-tenant by design',
    desc: 'Every org gets isolated data, API keys, and delivery stats. One platform — many clients.',
    rotate: '-0.5deg',
    accent: 'border-l-4 border-coral',
  },
  {
    title: 'Idempotent sends',
    desc: 'Pass an Idempotency-Key header and we guarantee the message is sent exactly once — no duplicates.',
    rotate: '0.8deg',
    accent: 'border-l-4 border-yellow',
  },
  {
    title: 'Live delivery status',
    desc: 'Real-time status updates over SignalR. See Pending to Sent without refreshing.',
    rotate: '-0.7deg',
    accent: 'border-l-4 border-violet',
  },
  {
    title: 'HTML email templates',
    desc: 'Build and store rich HTML templates with placeholder support. Send pixel-perfect emails every time.',
    rotate: '0.5deg',
    accent: 'border-l-4 border-teal',
  },
]

const steps = [
  { step: '01', title: 'Sign up', desc: 'Create your account and org in 30 seconds.' },
  { step: '02', title: 'Get your API key', desc: 'Copy your key from Settings — shown once, store it securely.' },
  { step: '03', title: 'Send a notification', desc: 'POST to /api/v1/notifications with your key and payload.' },
  { step: '04', title: 'Track delivery', desc: 'Watch real-time status in the dashboard.' },
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
          href="/login"
          className="border border-ink/20 px-6 py-3.5 rounded-full text-sm font-semibold hover:bg-fog transition"
        >
          Log in
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
  "type": "WelcomeEmail",
  "channel": "Email",
  "payload": {
    "subject": "Welcome aboard!",
    "body": "Thanks for signing up."
  }
}`}</pre>
          <div className="mt-4 pt-4 border-t border-paper/10 flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-teal animate-pulse" />
            <span className="text-teal text-xs font-mono">201 — queued in 12ms</span>
          </div>
        </div>
      </section>

      {/* Stats */}
      <section className="grid grid-cols-3 gap-4 py-8">
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
      <section className="py-16">
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