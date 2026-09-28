import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { AppRouter } from "./app/router.tsx";
import { AuthProvider } from "./auth/AuthContext.tsx";
import { PreferencesProvider } from "./shared/preferences/Preferences.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <PreferencesProvider>
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </PreferencesProvider>
  </StrictMode>,
);
