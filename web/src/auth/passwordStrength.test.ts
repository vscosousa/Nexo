import { describe, expect, it } from "vitest";
import { improvementFor, measurePassword } from "./passwordStrength";

describe("measurePassword", () => {
  it.each([
    ["Sh0rt!a", "weak", "short"],
    ["abcdefgh", "weak", "kinds"],
    ["ALLUPPERCASE", "weak", "kinds"],
    ["abcdefghijklmno", "weak", "kinds"],
    ["Abcdefg1", "reasonable", undefined],
    ["abcdef1!", "reasonable", undefined],
    ["Abcdefg1!", "reasonable", undefined],
    ["NoSymbols1234Ab", "reasonable", undefined],
    ["abcdefghijklmnop", "reasonable", undefined],
    ["abcdefghijklmnopqrs", "reasonable", undefined],
    ["Abcdefgh1!xy", "strong", undefined],
    ["Str0ng!Passw0rd", "strong", undefined],
    ["NoSymbols1234Abcd", "strong", undefined],
    ["correct horse battery", "strong", undefined],
    ["Str0ng!Passw0rd!!", "veryStrong", undefined],
    ["NoSymbols1234Abcdefgh", "veryStrong", undefined],
    ["correct horse battery staple", "veryStrong", undefined],
  ])(
    "given %s, when measured, then it is %s (the same table as the API's PasswordPolicyTests)",
    (password, strength, reason) => {
      expect(measurePassword(password)).toEqual({ strength, reason });
    },
  );

  it("given a long password containing a name, when measured, then it is weak because of the name", () => {
    expect(measurePassword("Xx!AnaAdmin9-long-enough", "Ana Admin")).toEqual({
      strength: "weak",
      reason: "names",
    });
  });

  it("given a name in look-alike characters, when measured, then it is still caught", () => {
    expect(measurePassword("Xx!L0c@lCl_ub9", "Local Club").reason).toBe(
      "names",
    );
  });

  it("given short or missing names, when measured, then they are ignored", () => {
    expect(
      measurePassword("Str0ng!Passw0rd", undefined, "  ", "Al", "Bo Li"),
    ).toEqual({ strength: "strong", reason: undefined });
  });
});

describe("improvementFor", () => {
  it.each([
    ["Secret12", { kind: "symbol" }],
    ["secret1!", { kind: "upper" }],
    ["Secret!!", { kind: "digit" }],
    ["SECRET1!", { kind: "lower" }],
    ["Secret-1", { length: 12 }],
    ["Str0ng!Passw0rd", { length: 16 }],
    ["correct horse battery", { kind: "digit" }],
  ])(
    "given %s, when asking how to strengthen it, then the missing kind comes first (symbols first), then length",
    (password, next) => {
      expect(improvementFor(password)).toEqual(next);
    },
  );
});
