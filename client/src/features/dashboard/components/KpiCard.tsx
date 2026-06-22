type KpiCardProps = {
  label: string
  value: string | number
  accent: 'violet' | 'coral' | 'teal' | 'yellow'
  rotate?: string
}

const accentText: Record<KpiCardProps['accent'], string> = {
  violet: 'text-violet',
  coral: 'text-coral',
  teal: 'text-teal',
  yellow: 'text-ink',
}

export default function KpiCard({ label, value, accent, rotate = '-1deg' }: KpiCardProps) {
  return (
    <div
      className="bg-fog border border-ink/10 rounded-lg p-5 transition hover:-translate-y-1.5 hover:shadow-lg"
      style={{ transform: `rotate(${rotate})` }}
    >
      <p className={`font-display font-extrabold text-3xl sm:text-4xl ${accentText[accent]}`}>
        {value}
      </p>
      <p className="text-xs sm:text-sm text-ink/60 mt-1">{label}</p>
    </div>
  )
}