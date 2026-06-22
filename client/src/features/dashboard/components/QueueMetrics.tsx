export default function QueueMetrics() {
  return (
    <div className="lg:col-span-2 bg-fog border border-ink/10 rounded-lg p-6 min-h-[280px]">
      <h2 className="font-display font-bold text-lg mb-1">Notification Volume</h2>
      <p className="text-sm text-ink/50 mb-6">Last 7 days · Email / SMS / Push</p>
      <div className="h-48 flex items-center justify-center text-ink/30 text-sm border border-dashed border-ink/15 rounded-lg">
        Chart goes here once the API is live
      </div>
    </div>
  )
}