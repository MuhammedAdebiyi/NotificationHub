import AuthLayout from '@/app/layouts/AuthLayout'
import ResetPasswordForm from '../components/ResetPasswordForm'

export default function ResetPasswordPage() {
  return (
    <AuthLayout>
      <div className="max-w-md mx-auto py-16">
        <p className="hand text-xl text-violet mb-1">almost done —</p>
        <h1 className="font-display font-bold text-3xl mb-8">Set a new password</h1>
        <ResetPasswordForm />
      </div>
    </AuthLayout>
  )
}