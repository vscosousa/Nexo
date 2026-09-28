import { usePreferences } from "../shared/preferences/Preferences";
import { Link } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { RegisterForm } from "./RegisterForm";

/** Registration screen for a new organization and its admin. */
export function RegisterPage() {
  const { m } = usePreferences();
  return (
    <AuthLayout title={m.auth.registerTitle} subtitle={m.auth.registerSubtitle}>
      <RegisterForm />
      <p className="auth-footer">
        {m.auth.haveAccount} <Link to="/login">{m.auth.signInLink}</Link>
      </p>
    </AuthLayout>
  );
}
