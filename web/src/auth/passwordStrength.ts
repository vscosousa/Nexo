/** How hard a password is to guess; `weak` is refused by the API. */
export type PasswordStrength = "weak" | "reasonable" | "strong" | "veryStrong";

/** Why a password is weak, so the form can say how to fix it. */
export type WeakReason = "short" | "names" | "kinds";

/** A kind of character, in the order they are suggested when missing. */
export type CharacterKind = "symbol" | "digit" | "upper" | "lower";

const MIN_LENGTH = 8;
const LENGTH_STEPS = [12, 16, 20, 24];
const PASSPHRASE_LENGTH = 16;
const MIN_KINDS = 3;
const MIN_TERM_LENGTH = 3;

const LOOK_ALIKES: Record<string, string> = {
  "0": "o",
  "1": "l",
  i: "l",
  "|": "l",
  "3": "e",
  "4": "a",
  "@": "a",
  "5": "s",
  $: "s",
  "7": "t",
};

/** Lowercases, maps look-alike characters onto one letter, and drops everything but letters and digits. */
function fold(value: string): string {
  return [...value.toLowerCase()]
    .map((c) => LOOK_ALIKES[c] ?? c)
    .filter((c) => /[\p{L}\p{Nd}]/u.test(c))
    .join("");
}

const KIND_PATTERNS: [CharacterKind, RegExp][] = [
  ["symbol", /[^\p{L}\p{Nd}]/u],
  ["digit", /\p{Nd}/u],
  ["upper", /\p{Lu}/u],
  ["lower", /\p{Ll}/u],
];

function missingKinds(password: string): CharacterKind[] {
  return KIND_PATTERNS.filter(([, pattern]) => !pattern.test(password)).map(
    ([kind]) => kind,
  );
}

function kinds(password: string): number {
  return KIND_PATTERNS.length - missingKinds(password).length;
}

function containsName(password: string, names: (string | undefined)[]) {
  const folded = fold(password);
  return names
    .filter((name): name is string => !!name?.trim())
    .flatMap((name) => [...name.trim().split(/\s+/), name])
    .map(fold)
    .some((term) => term.length >= MIN_TERM_LENGTH && folded.includes(term));
}

/**
 * Rates a password the same way the API's `PasswordPolicy.Measure` does, so the strength bar agrees with what the
 * API accepts. Weak (refused) under 8 characters, with a name in it, or with fewer than 3 kinds of character
 * (upper, lower, digit, symbol) unless it is a passphrase of 16+ characters. Otherwise it scores length points
 * (one each from 12, 16, 20 and 24 characters) plus (kinds - 3), so each kind is worth about four characters:
 * reasonable up to 1, strong at 2, very strong from 3.
 *
 * @param names Names the password must not contain (the person's, the organization's), compared ignoring case,
 *   spacing, punctuation, and look-alike characters such as `0`/`o` or `@`/`a`.
 * @returns The strength, and for a weak password the reason to show.
 */
export function measurePassword(
  password: string,
  ...names: (string | undefined)[]
): { strength: PasswordStrength; reason?: WeakReason } {
  if (password.length < MIN_LENGTH)
    return { strength: "weak", reason: "short" };
  if (containsName(password, names))
    return { strength: "weak", reason: "names" };
  const variety = kinds(password);
  if (variety < MIN_KINDS && password.length < PASSPHRASE_LENGTH)
    return { strength: "weak", reason: "kinds" };
  const score =
    LENGTH_STEPS.filter((step) => password.length >= step).length +
    variety -
    MIN_KINDS;
  return {
    strength: score >= 3 ? "veryStrong" : score === 2 ? "strong" : "reasonable",
    reason: undefined,
  };
}

/**
 * The quickest way to make an acceptable password stronger: add a kind of character it lacks (symbols first, since
 * they add the most), or else grow it to the next length step.
 *
 * @returns The missing kind to add, or the length to reach.
 */
export function improvementFor(
  password: string,
): { kind: CharacterKind } | { length: number } {
  const [missing] = missingKinds(password);
  if (missing) return { kind: missing };
  const next = LENGTH_STEPS.find((step) => password.length < step);
  return { length: next ?? password.length + 4 };
}
