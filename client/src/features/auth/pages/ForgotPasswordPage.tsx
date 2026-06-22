import AuthLayout from '@/app/layouts/AuthLayout'
import ForgotPasswordForm from '../components/ForgotPasswordForm'

export default function ForgotPasswordPage() {
  return (
    <AuthLayout>
      <div className="max-w-md mx-auto py-16">
        <p className="hand text-xl text-coral mb-1">forgot something? —</p>
        <h1 className="font-display font-bold text-3xl mb-8">Reset your password</h1>
        <ForgotPasswordForm />
      </div>
    </AuthLayout>
  )
}