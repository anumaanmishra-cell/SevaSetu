import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./auth/AuthContext";
import { GuestOnly, RequireRole } from "./auth/guards";
import { RoleLayout } from "./layouts/RoleLayout";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import {
  AdminAudit,
  AdminHome,
  AgencyHome,
  CaregiverHome,
  ElderHome,
  EpisodeDesk,
  FamilyBills,
  FamilyHome,
  FamilySearch,
  GstForm,
  HospitalHome,
  InboxPage
} from "./pages/AppPages";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<GuestOnly />}>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
          </Route>
          <Route element={<RequireRole role="Family" />}>
            <Route element={<RoleLayout title="Family" links={[{ to: "/family", label: "home" }, { to: "/family/search", label: "find" }, { to: "/family/bills", label: "bills" }]} />}>
              <Route path="/family" element={<FamilyHome />} />
              <Route path="/family/search" element={<FamilySearch />} />
              <Route path="/family/bills" element={<FamilyBills />} />
              <Route path="/family/episodes/:id" element={<EpisodeDesk prefix="/family" />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="Caregiver" />}>
            <Route element={<RoleLayout title="Caregiver" links={[{ to: "/caregiver", label: "shifts" }, { to: "/caregiver/inbox", label: "jobs" }, { to: "/caregiver/gst", label: "gst" }]} />}>
              <Route path="/caregiver" element={<CaregiverHome />} />
              <Route path="/caregiver/inbox" element={<InboxPage />} />
              <Route path="/caregiver/gst" element={<GstForm />} />
              <Route path="/caregiver/episodes/:id" element={<EpisodeDesk prefix="/caregiver" />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="AgencyAdmin" />}>
            <Route element={<RoleLayout title="Agency" links={[{ to: "/agency", label: "desk" }, { to: "/agency/inbox", label: "inbox" }, { to: "/agency/gst", label: "gst" }]} />}>
              <Route path="/agency" element={<AgencyHome />} />
              <Route path="/agency/inbox" element={<InboxPage />} />
              <Route path="/agency/gst" element={<GstForm />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="HospitalCoordinator" />}>
            <Route element={<RoleLayout title="Hospital" links={[{ to: "/hospital", label: "discharge" }]} />}>
              <Route path="/hospital" element={<HospitalHome />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="PlatformAdmin" />}>
            <Route element={<RoleLayout title="Admin" links={[{ to: "/admin", label: "kyc" }, { to: "/admin/audit", label: "audit" }]} />}>
              <Route path="/admin" element={<AdminHome />} />
              <Route path="/admin/audit" element={<AdminAudit />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="Elder" />}>
            <Route element={<RoleLayout title="Elder" links={[{ to: "/elder", label: "elderHome" }]} />}>
              <Route path="/elder" element={<ElderHome />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
