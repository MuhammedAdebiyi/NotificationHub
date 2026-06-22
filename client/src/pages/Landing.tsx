import AuthLayout from '@/app/layouts/AuthLayout'

export default function Landing() {
  return (
    <AuthLayout>
      <section className="text-center py-16 sm:py-24">
        <p className="hand text-2xl text-violet mb-2">notifications, handled properly —</p>
        <h1 className="font-display font-extrabold text-4xl sm:text-6xl leading-tight">
          One platform.<br />Every channel.
        </h1>
        <p className="mt-6 text-ink/70 max-w-xl mx-auto text-lg leading-relaxed">
          Email, SMS, push, and in-app notifications — queued, tracked, and
          retried automatically. Built for systems that can't afford to drop a message.
        </p>
        <div className="mt-8 flex flex-wrap justify-center gap-3">
          <a href="/signup" className="bg-ink text-white px-5 py-3 rounded-full text-sm font-semibold hover:bg-violet transition">
            Get started
          </a>
          <a href="/login" className="border border-ink/20 px-5 py-3 rounded-full text-sm font-semibold hover:bg-fog transition">
            Log in
          </a>
        </div>
      </section>

      <section className="grid sm:grid-cols-3 gap-5 py-10">
        <div className="bg-fog border border-ink/10 rounded-lg p-6 rotate-[-1deg]">
          <h3 className="font-display font-bold text-lg mb-1">Queued, not blocking</h3>
          <p className="text-sm text-ink/60">Every request returns instantly. Workers handle delivery in the background.</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-lg p-6 rotate-[1deg]">
          <h3 className="font-display font-bold text-lg mb-1">Retries + DLQ</h3>
          <p className="text-sm text-ink/60">Failed sends back off automatically and land in a dead letter queue for replay.</p>
        </div>
        <div className="bg-fog border border-ink/10 rounded-lg p-6 rotate-[-0.5deg]">
          <h3 className="font-display font-bold text-lg mb-1">Live status</h3>
          <p className="text-sm text-ink/60">Real-time delivery tracking over SignalR — no refreshing to see what happened.</p>
        </div>
      </section>
    </AuthLayout>
  )
}