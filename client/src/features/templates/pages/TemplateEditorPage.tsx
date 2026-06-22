import { useParams } from 'react-router-dom'
import AppLayout from '@/app/layouts/AppLayout'

export default function TemplateEditorPage() {
  const { id } = useParams()
  return (
    <AppLayout>
      <h1 className="font-display font-bold text-3xl mb-2">Edit Template #{id}</h1>
      <p className="text-ink/50">Rich editor + live preview panel.</p>
    </AppLayout>
  )
}