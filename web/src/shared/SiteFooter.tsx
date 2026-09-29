import { Link } from "react-router-dom";
import { usePreferences, PreferenceToggles } from "./preferences/Preferences";
import "./site-footer.css";

function Brand() {
  return (
    <Link to="/" className="brand" aria-label="Nexo">
      <span className="brand-mark" aria-hidden="true" />
      <span className="brand-word" aria-hidden="true" />
    </Link>
  );
}

/** Footer shared by every public page: brand blurb, link columns, copyright, and the language/theme toggles. */
export function SiteFooter() {
  const { m } = usePreferences();
  const t = m.landing;
  return (
    <footer className="site-footer">
      <div className="site-footer-top">
        <div className="site-footer-about">
          <Brand />
          <p>{t.footerAbout}</p>
        </div>
        {t.footerColumns.map((column) => (
          <div key={column.title} className="site-footer-col">
            <h3>{column.title}</h3>
            <ul>
              {column.links.map((link) => (
                <li key={link}>{link}</li>
              ))}
            </ul>
          </div>
        ))}
      </div>
      <div className="site-copyright">
        <p>{t.copyright}</p>
        <PreferenceToggles />
      </div>
    </footer>
  );
}
