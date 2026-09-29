import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { ActivateAccountPage } from "../auth/ActivateAccountPage";
import { ConfirmEmailPage, UnlockAccountPage } from "../auth/EmailLinkPage";
import { LoginCallbackPage } from "../auth/LoginCallbackPage";
import { LoginPage } from "../auth/LoginPage";
import { RegisterPage } from "../auth/RegisterPage";
import { ChoosePlanPage } from "../plans/ChoosePlanPage";
import { LandingPage } from "../landing/LandingPage";
import { RequireAuth } from "../auth/RequireAuth";
import { RequireSearchParam } from "../shared/routing/RequireSearchParam";
import { ScrollToTop } from "../shared/routing/ScrollToTop";
import App from "./App";

export function AppRouter() {
  return (
    <BrowserRouter>
      <ScrollToTop />
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<ChoosePlanPage />} />
        <Route
          path="/register/organization"
          element={
            <RequireSearchParam name="planId" redirectTo="/register">
              <RegisterPage />
            </RequireSearchParam>
          }
        />
        <Route
          path="/activate"
          element={
            <RequireSearchParam name="token">
              <RequireSearchParam name="email">
                <ActivateAccountPage />
              </RequireSearchParam>
            </RequireSearchParam>
          }
        />
        <Route
          path="/confirm-email"
          element={
            <RequireSearchParam name="token">
              <RequireSearchParam name="email">
                <ConfirmEmailPage />
              </RequireSearchParam>
            </RequireSearchParam>
          }
        />
        <Route
          path="/unlock"
          element={
            <RequireSearchParam name="token">
              <RequireSearchParam name="email">
                <UnlockAccountPage />
              </RequireSearchParam>
            </RequireSearchParam>
          }
        />
        <Route path="/login/callback" element={<LoginCallbackPage />} />
        <Route
          path="/app"
          element={
            <RequireAuth>
              <App />
            </RequireAuth>
          }
        />
        {/* Unknown paths and forbidden ones (RequireAuth/RequireSearchParam) both land here. */}
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
