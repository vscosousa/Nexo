import { usePreferences } from "../shared/preferences/Preferences";
import { AuthLayout } from "./AuthLayout";
import { ActivateAccountForm } from "./ActivateAccountForm";

/** Landing page of the invitation email link: an invited person creates their member account. */
export function ActivateAccountPage() {
  const { m } = usePreferences();
  return (
    <AuthLayout title={m.auth.activateTitle} subtitle={m.auth.activateSubtitle}>
      <ActivateAccountForm />
    </AuthLayout>
  );
}
