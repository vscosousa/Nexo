import { Check, ChevronDown } from "lucide-react";
import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type KeyboardEvent,
} from "react";

export interface SelectOption {
  value: string;
  label: string;
}

/**
 * Labelled single-choice field drawn by the app instead of the browser's native list, following the WAI-ARIA
 * "select-only combobox" pattern: arrows, Home/End, and typing a letter move through the options, Enter/Space or
 * Tab pick the active one, and Escape closes without choosing (without also closing a surrounding dialog). The
 * choice is submitted with the form through a hidden input named `name`. The list is fixed-positioned under the
 * box, so a scrolling container (such as a dialog body) cannot clip it.
 */
export function Select({
  label,
  name,
  options,
  placeholder,
  error,
  defaultValue = "",
}: {
  label: string;
  name: string;
  options: SelectOption[];
  placeholder: string;
  error?: string;
  defaultValue?: string;
}) {
  const id = useId();
  const boxRef = useRef<HTMLButtonElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const [value, setValue] = useState(defaultValue);
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const [position, setPosition] = useState({ top: 0, left: 0, width: 0 });
  const selected = options.find((option) => option.value === value);
  const optionId = (index: number) => `${id}-option-${index}`;

  const show = (index: number) => {
    setActive(Math.max(0, Math.min(index, options.length - 1)));
    setOpen(true);
  };

  const choose = (index: number) => {
    if (options[index]) setValue(options[index].value);
    setOpen(false);
  };

  const indexStartingWith = (key: string) => {
    const start = open ? active + 1 : 0;
    const ordered = [...options.slice(start), ...options.slice(0, start)];
    const match = ordered.find((option) =>
      option.label.toLowerCase().startsWith(key.toLowerCase()),
    );
    return match ? options.indexOf(match) : -1;
  };

  useLayoutEffect(() => {
    if (!open || !boxRef.current) return;
    const box = boxRef.current.getBoundingClientRect();
    setPosition({ top: box.bottom + 4, left: box.left, width: box.width });
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const close = (event: Event) => {
      const target = event.target as Node;
      if (
        !boxRef.current?.contains(target) &&
        !listRef.current?.contains(target)
      )
        setOpen(false);
    };
    const closeOnMove = (event: Event) => {
      const inList =
        event.target instanceof Node && listRef.current?.contains(event.target);
      if (!inList) setOpen(false);
    };
    document.addEventListener("pointerdown", close);
    window.addEventListener("resize", closeOnMove);
    window.addEventListener("scroll", closeOnMove, true);
    return () => {
      document.removeEventListener("pointerdown", close);
      window.removeEventListener("resize", closeOnMove);
      window.removeEventListener("scroll", closeOnMove, true);
    };
  }, [open]);

  useEffect(() => {
    const list = listRef.current;
    const option = list?.querySelector<HTMLElement>(
      `[id="${optionId(active)}"]`,
    );
    if (!open || !list || !option) return;
    if (option.offsetTop < list.scrollTop) list.scrollTop = option.offsetTop;
    else if (
      option.offsetTop + option.offsetHeight >
      list.scrollTop + list.clientHeight
    )
      list.scrollTop =
        option.offsetTop + option.offsetHeight - list.clientHeight;
  });

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>) => {
    const current = options.findIndex((option) => option.value === value);
    if (!open) {
      if (["ArrowDown", "ArrowUp", "Enter", " "].includes(event.key)) {
        event.preventDefault();
        show(current >= 0 ? current : 0);
      } else if (event.key === "Home" || event.key === "End") {
        event.preventDefault();
        show(event.key === "Home" ? 0 : options.length - 1);
      } else if (event.key.length === 1 && /\S/.test(event.key)) {
        const match = indexStartingWith(event.key);
        if (match >= 0) setValue(options[match].value);
      }
      return;
    }
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        setActive((index) => Math.min(index + 1, options.length - 1));
        break;
      case "ArrowUp":
        event.preventDefault();
        if (event.altKey) choose(active);
        else setActive((index) => Math.max(index - 1, 0));
        break;
      case "Home":
      case "End":
        event.preventDefault();
        setActive(event.key === "Home" ? 0 : options.length - 1);
        break;
      case "Enter":
      case " ":
        event.preventDefault();
        choose(active);
        break;
      case "Tab":
        choose(active);
        break;
      case "Escape":
        event.preventDefault();
        event.stopPropagation();
        setOpen(false);
        break;
      default:
        if (event.key.length === 1 && /\S/.test(event.key)) {
          const match = indexStartingWith(event.key);
          if (match >= 0) setActive(match);
        }
    }
  };

  return (
    <div className="field">
      <label id={`${id}-label`} htmlFor={`${id}-box`}>
        {label}
      </label>
      <input type="hidden" name={name} value={value} />
      <button
        ref={boxRef}
        id={`${id}-box`}
        type="button"
        role="combobox"
        className="select-box"
        aria-labelledby={`${id}-label`}
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={`${id}-list`}
        aria-activedescendant={open ? optionId(active) : undefined}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${id}-error` : undefined}
        onClick={() =>
          open
            ? setOpen(false)
            : show(
                Math.max(
                  0,
                  options.findIndex((o) => o.value === value),
                ),
              )
        }
        onKeyDown={onKeyDown}
      >
        <span className={selected ? undefined : "select-placeholder"}>
          {selected?.label ?? placeholder}
        </span>
        <ChevronDown aria-hidden="true" />
      </button>
      {open && (
        <ul
          ref={listRef}
          id={`${id}-list`}
          role="listbox"
          aria-labelledby={`${id}-label`}
          className="select-list"
          style={position}
        >
          {options.map((option, index) => (
            <li
              key={option.value}
              id={optionId(index)}
              role="option"
              aria-selected={option.value === value}
              className={index === active ? "is-active" : undefined}
              onPointerMove={() => setActive(index)}
              onPointerDown={(event) => event.preventDefault()}
              onClick={() => {
                choose(index);
                boxRef.current?.focus();
              }}
            >
              <span>{option.label}</span>
              {option.value === value && <Check aria-hidden="true" />}
            </li>
          ))}
        </ul>
      )}
      {error && (
        <p id={`${id}-error`} className="field-error">
          {error}
        </p>
      )}
    </div>
  );
}
