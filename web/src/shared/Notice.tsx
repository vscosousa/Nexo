import type { ReactNode } from "react";
import { CircleAlert, CircleCheck } from "lucide-react";

/**
 * One short message about the outcome of an action: an error (announced immediately, as `role="alert"`) or a
 * success (announced politely, as `role="status"`). The icon carries the tone, so the message stays quiet: a
 * faint tint, no border.
 */
export function Notice({
  tone,
  children,
}: {
  tone: "error" | "success";
  children: ReactNode;
}) {
  const Icon = tone === "error" ? CircleAlert : CircleCheck;
  return (
    <p
      role={tone === "error" ? "alert" : "status"}
      className={`notice notice-${tone}`}
    >
      <Icon aria-hidden="true" />
      <span>{children}</span>
    </p>
  );
}
