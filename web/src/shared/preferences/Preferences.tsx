import {
  createContext,
  useContext,
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { messages, type Lang } from "./messages";

type Theme = "light" | "dark";

interface Preferences {
  lang: Lang;
  setLang: (lang: Lang) => void;
  theme: Theme;
  toggleTheme: () => void;
}

const Ctx = createContext<Preferences>({
  lang: "en",
  setLang: () => {},
  theme: "light",
  toggleTheme: () => {},
});

const read = (key: string) => {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
};

const systemLang = (): Lang =>
  navigator.language.toLowerCase().startsWith("pt") ? "pt" : "en";

const darkQuery = () => window.matchMedia?.("(prefers-color-scheme: dark)");

const save = (key: string, value: string) => {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* empty */
  }
};

/**
 * Holds the language and theme and mirrors them onto `<html>`. Both follow the system
 * (browser language, OS theme) until the user picks one; only an explicit pick is saved.
 */
export function PreferencesProvider({ children }: { children: ReactNode }) {
  const [langChoice, setLangChoice] = useState(() => read("lang"));
  const [themeChoice, setThemeChoice] = useState(() => read("theme"));
  const [systemDark, setSystemDark] = useState(
    () => darkQuery()?.matches ?? false,
  );

  useEffect(() => {
    const query = darkQuery();
    const onChange = () => setSystemDark(query.matches);
    query?.addEventListener("change", onChange);
    return () => query?.removeEventListener("change", onChange);
  }, []);

  const lang: Lang =
    langChoice === "pt" || langChoice === "en" ? langChoice : systemLang();
  const theme: Theme =
    themeChoice === "light" || themeChoice === "dark"
      ? themeChoice
      : systemDark
        ? "dark"
        : "light";

  useEffect(() => {
    document.documentElement.lang = lang;
    document.documentElement.dataset.theme = theme;
  }, [lang, theme]);

  const setLang = (next: Lang) => {
    setLangChoice(next);
    save("lang", next);
  };
  const toggleTheme = () => {
    const next = theme === "dark" ? "light" : "dark";
    setThemeChoice(next);
    save("theme", next);
  };

  return (
    <Ctx.Provider value={{ lang, setLang, theme, toggleTheme }}>
      {children}
    </Ctx.Provider>
  );
}

/** Current language, its copy (`m`) and the theme controls. */
export function usePreferences() {
  const prefs = useContext(Ctx);
  return { ...prefs, m: messages[prefs.lang] };
}

/**
 * Language switch, shown on every public page. Switching languages changes text
 * lengths above the fold, which reflows the page; this keeps the toggle itself
 * anchored on screen instead of jumping out from under the reader.
 */
export function PreferenceToggles() {
  const { lang, setLang, m } = usePreferences();
  const groupRef = useRef<HTMLDivElement>(null);
  const anchorTop = useRef<number | null>(null);

  useLayoutEffect(() => {
    if (anchorTop.current === null || !groupRef.current) return;
    const after = groupRef.current.getBoundingClientRect().top;
    window.scrollBy(0, after - anchorTop.current);
    anchorTop.current = null;
  }, [lang]);

  const handleLang = (code: Lang) => {
    anchorTop.current = groupRef.current?.getBoundingClientRect().top ?? null;
    setLang(code);
  };

  return (
    <div className="prefs">
      <div
        className="segmented"
        role="group"
        aria-label={m.common.language}
        ref={groupRef}
      >
        {(["pt", "en"] as const).map((code) => (
          <button
            key={code}
            type="button"
            aria-pressed={lang === code}
            onClick={() => handleLang(code)}
          >
            {code.toUpperCase()}
          </button>
        ))}
      </div>
    </div>
  );
}
