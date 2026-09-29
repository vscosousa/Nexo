import { useEffect, useRef, useState } from "react";

/**
 * One box per character, for entering a short invitation code without a big blank text field.
 * Pasting anywhere distributes the clipboard text across the boxes in order, starting from that box.
 */
export function CodeInput({
  name,
  length,
  label,
  onChange,
  disabled,
}: {
  name: string;
  length: number;
  label: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  const [chars, setChars] = useState<string[]>(() => Array(length).fill(""));
  const boxes = useRef<(HTMLInputElement | null)[]>([]);

  useEffect(() => {
    boxes.current[0]?.focus();
  }, []);

  const apply = (next: string[]) => {
    setChars(next);
    onChange(next.every((c) => c) ? next.join("") : "");
  };

  const handleChange =
    (index: number) => (event: React.ChangeEvent<HTMLInputElement>) => {
      const value = event.target.value.slice(-1);
      const next = [...chars];
      next[index] = value;
      apply(next);
      if (value && index < length - 1) boxes.current[index + 1]?.focus();
    };

  const handleKeyDown =
    (index: number) => (event: React.KeyboardEvent<HTMLInputElement>) => {
      if (event.key === "Backspace" && !chars[index] && index > 0) {
        boxes.current[index - 1]?.focus();
        const next = [...chars];
        next[index - 1] = "";
        apply(next);
      }
    };

  const handlePaste =
    (index: number) => (event: React.ClipboardEvent<HTMLInputElement>) => {
      event.preventDefault();
      const pasted = event.clipboardData.getData("text").replace(/\s/g, "");
      const next = [...chars];
      let i = index;
      for (const char of pasted) {
        if (i >= length) break;
        next[i] = char;
        i++;
      }
      apply(next);
      boxes.current[Math.min(i, length - 1)]?.focus();
    };

  return (
    <div className="field field-code">
      <label htmlFor={`${name}-0`}>{label}</label>
      <input type="hidden" name={name} value={chars.join("")} />
      <div className="code-input" aria-disabled={disabled}>
        {chars.map((char, index) => (
          <input
            key={index}
            id={`${name}-${index}`}
            ref={(el) => {
              boxes.current[index] = el;
            }}
            className={char ? "code-box code-box-filled" : "code-box"}
            type="password"
            value={char}
            onChange={handleChange(index)}
            onKeyDown={handleKeyDown(index)}
            onPaste={handlePaste(index)}
            maxLength={1}
            autoComplete="off"
            disabled={disabled}
            aria-label={`${label} ${index + 1}/${length}`}
          />
        ))}
      </div>
    </div>
  );
}
