import {
  CalendarDays,
  Euro,
  Handshake,
  Package,
  ScrollText,
  Wrench,
} from "lucide-react";
import { useEffect, useRef, type CSSProperties, type RefObject } from "react";
import { Link } from "react-router-dom";
import {
  PreferenceToggles,
  usePreferences,
} from "../shared/preferences/Preferences";
import heroImage from "./images/hero.jpg";
import step1Image from "./images/step-1.jpg";
import step2Image from "./images/step-2.jpg";
import step3Image from "./images/step-3.jpg";
import arco from "./partners/arco.svg";
import aurora from "./partners/aurora.svg";
import centroCultural from "./partners/centro-cultural.svg";
import cooperativa from "./partners/cooperativa.svg";
import horizonte from "./partners/horizonte.svg";
import nautico from "./partners/nautico.svg";
import ribeira from "./partners/ribeira.svg";
import valeVerde from "./partners/vale-verde.svg";
import "./landing.css";

const featureIcons = [
  CalendarDays,
  Package,
  Handshake,
  Wrench,
  Euro,
  ScrollText,
];
const stepImages = [step1Image, step2Image, step3Image];

// Fictional partners with placeholder marks (see partners/CREDITS.md); swap for real ones when they exist.
// `wordmark` is the invented text set beside the mark, `style` picks its type treatment and `color`
// is the mark's own color, which the whole lockup takes on hover.
const partners = [
  {
    logo: horizonte,
    name: "Associação Horizonte",
    wordmark: "Horizonte",
    style: "light",
    color: "#ff2121",
  },
  {
    logo: cooperativa,
    name: "Cooperativa Nascente",
    wordmark: "nascente",
    style: "bold",
    color: "#06835b",
  },
  {
    logo: valeVerde,
    name: "Câmara Municipal de Vale Verde",
    wordmark: "Vale Verde",
    style: "caps",
    color: "#09bcbf",
  },
  {
    logo: ribeira,
    name: "Casa do Povo da Ribeira",
    wordmark: "Ribeira",
    style: "caps",
    color: "#0094f7",
  },
  {
    logo: centroCultural,
    name: "Centro Cultural do Sol",
    wordmark: "Cultural Sol",
    style: "serif",
    color: "#348dfc",
  },
  {
    logo: aurora,
    name: "Clube Desportivo Aurora",
    wordmark: "Aurora",
    style: "heavy",
    color: "#6bda0a",
  },
  {
    logo: arco,
    name: "Escola de Música Arco",
    wordmark: "Arco",
    style: "bold",
    color: "#ff500b",
  },
  {
    logo: nautico,
    name: "Clube Náutico do Rio",
    wordmark: "Náutico",
    style: "light",
    color: "#2c4cfd",
  },
];

type Partner = (typeof partners)[number];

/** One partner lockup: the mark and its wordmark share a single hover state that fills in the mark color. */
function PartnerLogo({
  partner,
  hidden,
}: {
  partner: Partner;
  hidden?: boolean;
}) {
  return (
    <li style={{ "--logo-color": partner.color } as CSSProperties}>
      <img src={partner.logo} alt={hidden ? "" : partner.name} />
      <span
        className="lp-wordmark"
        data-style={partner.style}
        aria-hidden="true"
      >
        {partner.wordmark}
      </span>
    </li>
  );
}

/**
 * Lets the partner strip drift on its own but stay in the hands of the reader: it pauses while pointed
 * at or touched, scrolls freely in both directions (swipe or mouse drag), and wraps around the duplicated second list.
 */
function usePartnerMarquee(ref: RefObject<HTMLDivElement | null>) {
  useEffect(() => {
    const el = ref.current;
    if (!el || window.matchMedia?.("(prefers-reduced-motion: reduce)").matches)
      return;
    const half = () => el.scrollWidth / 2;
    let paused = false;
    let resume: number | undefined;
    el.scrollLeft = half();
    const wrap = () => {
      if (el.scrollLeft >= half()) el.scrollLeft -= half();
      else if (el.scrollLeft < 1) el.scrollLeft += half();
    };
    let frame = requestAnimationFrame(function tick() {
      if (!paused) el.scrollLeft += 1;
      frame = requestAnimationFrame(tick);
    });
    const pause = () => {
      paused = true;
      window.clearTimeout(resume);
    };
    const play = (delay = 0) => {
      window.clearTimeout(resume);
      resume = window.setTimeout(() => (paused = false), delay);
    };
    const onLeave = () => play();
    let dragging = false;
    let lastX = 0;
    const onDown = (e: PointerEvent) => {
      if (e.pointerType !== "mouse") return;
      dragging = true;
      lastX = e.clientX;
      el.setPointerCapture(e.pointerId);
    };
    const onMove = (e: PointerEvent) => {
      if (!dragging) return;
      el.scrollLeft -= e.clientX - lastX;
      lastX = e.clientX;
    };
    const onUp = () => (dragging = false);
    const onTouchEnd = () => play(2000);
    el.addEventListener("scroll", wrap);
    el.addEventListener("pointerenter", pause);
    el.addEventListener("pointerleave", onLeave);
    el.addEventListener("pointerdown", onDown);
    el.addEventListener("pointermove", onMove);
    el.addEventListener("pointerup", onUp);
    el.addEventListener("pointercancel", onUp);
    el.addEventListener("touchstart", pause, { passive: true });
    el.addEventListener("touchend", onTouchEnd);
    return () => {
      cancelAnimationFrame(frame);
      window.clearTimeout(resume);
      el.removeEventListener("scroll", wrap);
      el.removeEventListener("pointerenter", pause);
      el.removeEventListener("pointerleave", onLeave);
      el.removeEventListener("pointerdown", onDown);
      el.removeEventListener("pointermove", onMove);
      el.removeEventListener("pointerup", onUp);
      el.removeEventListener("pointercancel", onUp);
      el.removeEventListener("touchstart", pause);
      el.removeEventListener("touchend", onTouchEnd);
    };
  }, [ref]);
}

function Brand() {
  return (
    <a href="#" className="brand" aria-label="Nexo">
      <span className="brand-mark" aria-hidden="true" />
      <span className="brand-word" aria-hidden="true" />
    </a>
  );
}

/** Public landing page: header, hero, partners, features, how it works, call to action and footer. */
export function LandingPage() {
  const { m } = usePreferences();
  const t = m.landing;
  const marquee = useRef<HTMLDivElement>(null);
  usePartnerMarquee(marquee);
  return (
    <div className="landing">
      <header className="lp-header">
        <Brand />
        <nav aria-label={t.navLabel} className="lp-nav">
          <a href="#funcionalidades">{t.nav.features}</a>
          <a href="#como-funciona">{t.nav.how}</a>
          <a href="#parceiros">{t.nav.partners}</a>
          <a href="#sobre">{t.nav.about}</a>
        </nav>
        <div className="lp-actions">
          <Link to="/login" className="btn btn-secondary">
            {m.common.signIn}
          </Link>
          <Link to="/register" className="btn btn-primary">
            {m.common.getStarted}
          </Link>
        </div>
      </header>

      <main>
        <section className="lp-section lp-hero">
          <div className="lp-hero-text">
            <div className="lp-stack">
              <p className="lp-eyebrow">{t.eyebrow}</p>
              <h1>{t.title}</h1>
            </div>
            <p className="lp-lead">{t.lead}</p>
            <div className="lp-row">
              <Link to="/register" className="btn btn-primary btn-lg">
                {m.common.getStarted}
              </Link>
              <a href="#como-funciona" className="btn btn-secondary btn-lg">
                {t.seeHow}
              </a>
            </div>
          </div>
          <img
            className="lp-photo lp-photo-hero"
            src={heroImage}
            alt={t.heroAlt}
          />
        </section>

        <section id="parceiros" className="lp-trust">
          <p className="lp-eyebrow">{t.partnersEyebrow}</p>
          <div className="lp-marquee" ref={marquee}>
            <ul className="lp-partners" aria-label={t.partnersLabel}>
              {partners.map((p) => (
                <PartnerLogo key={p.name} partner={p} />
              ))}
            </ul>
            {/* Second copy makes the loop seamless; hidden from assistive tech. */}
            <ul className="lp-partners" aria-hidden="true">
              {partners.map((p) => (
                <PartnerLogo key={p.name} partner={p} hidden />
              ))}
            </ul>
          </div>
        </section>

        <section id="funcionalidades" className="lp-section">
          <div className="lp-heading lp-heading-center">
            <p className="lp-eyebrow">{t.featuresEyebrow}</p>
            <h2>{t.featuresTitle}</h2>
          </div>
          <ul className="lp-grid">
            {t.features.map((f, i) => {
              const Icon = featureIcons[i];
              return (
                <li key={f.title} className="lp-card">
                  <span className="lp-icon">
                    <Icon />
                  </span>
                  <div className="lp-stack">
                    <h3>{f.title}</h3>
                    <p>{f.text}</p>
                  </div>
                </li>
              );
            })}
          </ul>
        </section>

        <section id="como-funciona" className="lp-section lp-alt">
          <div className="lp-heading">
            <p className="lp-eyebrow">{t.stepsEyebrow}</p>
            <h2>{t.stepsTitle}</h2>
          </div>
          <ol className="lp-grid">
            {t.steps.map((s, i) => (
              <li key={s.title} className="lp-step">
                <img
                  className="lp-photo lp-photo-step"
                  src={stepImages[i]}
                  alt={s.alt}
                />
                <div className="lp-stack">
                  <h3>
                    <span className="lp-step-number">{i + 1}</span>
                    {s.title}
                  </h3>
                  <p>{s.text}</p>
                </div>
              </li>
            ))}
          </ol>
        </section>

        <section className="lp-section lp-cta-wrap">
          <div className="lp-cta">
            <div className="lp-stack">
              <h2>{t.ctaTitle}</h2>
              <p>{t.ctaText}</p>
            </div>
            <div className="lp-row">
              <Link to="/register" className="btn btn-primary btn-lg">
                {t.createAccount}
              </Link>
              <a href="#sobre" className="btn btn-secondary btn-lg">
                {t.contactUs}
              </a>
            </div>
          </div>
        </section>
      </main>

      <footer id="sobre" className="lp-footer">
        <div className="lp-footer-top">
          <div className="lp-footer-about">
            <Brand />
            <p>{t.footerAbout}</p>
          </div>
          {t.footerColumns.map((column) => (
            <div key={column.title} className="lp-footer-col">
              <h3>{column.title}</h3>
              <ul>
                {column.links.map((link) => (
                  <li key={link}>{link}</li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <div className="lp-copyright">
          <p>{t.copyright}</p>
          <PreferenceToggles />
        </div>
      </footer>
    </div>
  );
}
