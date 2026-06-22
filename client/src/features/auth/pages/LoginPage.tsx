import AuthLayout from '@/app/layouts/AuthLayout'
import LoginForm from '../components/Loginform'

export default function LoginPage() {
  return (
    <AuthLayout>
      <div className="max-w-md mx-auto py-12 sm:py-16">
        <p className="hand text-xl text-violet mb-1">welcome back —</p>
        <h1 className="font-display font-bold text-3xl mb-8">Log in</h1>
        <LoginForm />
        <p className="text-sm text-ink/60 mt-6 text-center">
          Don't have an account?{' '}
          <a href="/signup" className="text-violet font-medium hover:underline">Sign up</a>
        </p>
      </div>
    </AuthLayout>
  )
}