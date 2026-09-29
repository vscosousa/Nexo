import { X } from "lucide-react";
import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { Field } from "../../auth/Field";
import { describeApiError } from "../../shared/http/apiError";
import { Notice } from "../../shared/Notice";
import { Select } from "../../shared/Select";
import { usePreferences } from "../../shared/preferences/Preferences";
import {
  resourcesService,
  type Resource,
  type ResourceType,
} from "./resourcesService";

/**
 * The register-resource form, in a modal dialog opened on mount: name, a type from the organization's types
 * (system types shown in the current language, sorted by name with `Other` last), and an optional description.
 *
 * @param onClose Called once the dialog has closed (cancel, the close button, or Escape).
 * @param onRegistered Called with the created resource; the caller then closes the dialog.
 */
export function RegisterResourceForm({
  onClose,
  onRegistered,
}: {
  onClose: () => void;
  onRegistered: (resource: Resource) => void;
}) {
  const { m } = usePreferences();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const id = useId();
  const [types, setTypes] = useState<ResourceType[]>();
  const [typesFailed, setTypesFailed] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    dialogRef.current?.showModal();
    resourcesService.listTypes().then(setTypes, () => setTypesFailed(true));
  }, []);

  const typeName = (type: ResourceType) =>
    m.resources.typeNames[type.name] ?? type.name;
  const isOther = (type: ResourceType) => Number(type.name === "Other");
  const sortedTypes = [...(types ?? [])].sort(
    (a, b) => isOther(a) - isOther(b) || typeName(a).localeCompare(typeName(b)),
  );

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const name = String(form.get("name") ?? "").trim();
    const typeId = String(form.get("typeId") ?? "");
    const description = String(form.get("description") ?? "").trim();
    const missing: Record<string, string> = {};
    if (!name) missing.name = m.resources.nameRequired;
    if (!typeId) missing.typeId = m.resources.typeRequired;
    setFieldErrors(missing);
    setError("");
    if (Object.keys(missing).length > 0) return;

    setSubmitting(true);
    try {
      onRegistered(
        await resourcesService.register({
          name,
          typeId,
          description: description || undefined,
        }),
      );
    } catch (e) {
      const failure = describeApiError(e, m, {
        403: m.resources.forbidden,
        409: m.resources.limitReached,
      });
      setFieldErrors(failure.fieldErrors);
      setError(failure.message);
      setSubmitting(false);
    }
  };

  return (
    <dialog
      ref={dialogRef}
      className="app-dialog"
      aria-labelledby={`${id}-title`}
      onClose={onClose}
    >
      <form onSubmit={onSubmit} noValidate>
        <header className="app-dialog-header">
          <h2 id={`${id}-title`}>{m.resources.dialogTitle}</h2>
          <button
            type="button"
            className="icon-btn"
            aria-label={m.resources.close}
            onClick={() => dialogRef.current?.close()}
          >
            <X />
          </button>
        </header>
        <div className="app-dialog-body">
          <Field
            label={m.resources.name}
            name="name"
            error={fieldErrors.name}
          />
          <Select
            label={m.resources.type}
            name="typeId"
            placeholder={m.resources.typePlaceholder}
            options={sortedTypes.map((type) => ({
              value: type.id,
              label: typeName(type),
            }))}
            error={fieldErrors.typeId}
          />
          <div className="field">
            <label htmlFor={`${id}-description`}>
              {m.resources.description}
            </label>
            <textarea
              id={`${id}-description`}
              name="description"
              rows={3}
              aria-invalid={fieldErrors.description ? true : undefined}
              aria-describedby={
                fieldErrors.description ? `${id}-description-error` : undefined
              }
            />
            {fieldErrors.description && (
              <p id={`${id}-description-error`} className="field-error">
                {fieldErrors.description}
              </p>
            )}
          </div>
          {typesFailed && (
            <Notice tone="error">{m.resources.typesError}</Notice>
          )}
          {error && <Notice tone="error">{error}</Notice>}
        </div>
        <footer className="app-dialog-footer">
          <button
            type="button"
            className="btn btn-secondary"
            onClick={() => dialogRef.current?.close()}
          >
            {m.resources.cancel}
          </button>
          <button
            type="submit"
            className="btn btn-primary"
            disabled={submitting || !types}
          >
            {m.resources.submit}
          </button>
        </footer>
      </form>
    </dialog>
  );
}
