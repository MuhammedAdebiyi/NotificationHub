import ErrorBoundary from '@/shared/components/ErrorBoundary'
import AppRouter from '@/app/router/AppRouter'

function App() {
  return (
    <ErrorBoundary>
      <AppRouter />
    </ErrorBoundary>
  )
}

export default App
