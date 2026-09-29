import { renderHook } from "@testing-library/react";
import { createRef } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useScrollReveal } from "./useScrollReveal";

class FakeIntersectionObserver implements IntersectionObserver {
  static instances: FakeIntersectionObserver[] = [];
  root = null;
  rootMargin = "";
  scrollMargin = "";
  thresholds: ReadonlyArray<number> = [];
  observed: Element[] = [];
  callback: IntersectionObserverCallback;
  constructor(callback: IntersectionObserverCallback) {
    this.callback = callback;
    FakeIntersectionObserver.instances.push(this);
  }
  observe(target: Element) {
    this.observed.push(target);
  }
  unobserve(target: Element) {
    this.observed = this.observed.filter((el) => el !== target);
  }
  disconnect() {
    this.observed = [];
  }
  takeRecords() {
    return [];
  }
  intersect(target: Element) {
    this.callback(
      [{ target, isIntersecting: true } as IntersectionObserverEntry],
      this,
    );
  }
}

afterEach(() => {
  FakeIntersectionObserver.instances = [];
  vi.unstubAllGlobals();
});

describe("useScrollReveal", () => {
  it("when a marked element intersects, then it is revealed", () => {
    vi.stubGlobal("IntersectionObserver", FakeIntersectionObserver);
    const container = document.createElement("div");
    const target = document.createElement("div");
    target.dataset.reveal = "";
    container.appendChild(target);
    const ref = createRef<HTMLElement | null>();
    (ref as { current: HTMLElement }).current = container;

    renderHook(() => useScrollReveal(ref));
    expect(target.classList.contains("is-visible")).toBe(false);

    FakeIntersectionObserver.instances[0].intersect(target);
    expect(target.classList.contains("is-visible")).toBe(true);
  });

  it("when the browser has no IntersectionObserver, then elements are revealed immediately", () => {
    vi.stubGlobal("IntersectionObserver", undefined);
    const container = document.createElement("div");
    const target = document.createElement("div");
    target.dataset.reveal = "";
    container.appendChild(target);
    const ref = createRef<HTMLElement | null>();
    (ref as { current: HTMLElement }).current = container;

    renderHook(() => useScrollReveal(ref));

    expect(target.classList.contains("is-visible")).toBe(true);
  });
});
