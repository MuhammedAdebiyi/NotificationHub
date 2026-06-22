import AuthLayout from '@/layouts/AuthLayout'
import SignupForm from '../components/SignupForm'

export default function SignupPage() {
  return (
    <AuthLayout>
      <div className="max-w-md mx-auto py-12 sm:py-16">
        <p className="hand text-xl text-coral mb-1">let's get started —</p>
        <h1 className="font-display font-bold text-3xl mb-8">Create your account</h1>
        <SignupForm />
        <p className="text-sm text-ink/60 mt-6 text-center">
          Already have an account?{' '}
          <a href="/login" className="text-violet font-medium hover:underline">Log in</a>
        </p>
      </div>
    </AuthLayout>
  )
}