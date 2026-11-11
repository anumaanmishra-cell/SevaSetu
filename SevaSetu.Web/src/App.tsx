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
  EpisodeDesk,
  FamilyBills,
  FamilyHome,
  FamilySearch,
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
            <Route element={<RoleLayout title="Family" links={[{ to: "/family", label: "Home" }, { to: "/family/search", label: "Find" }, { to: "/family/bills", label: "Bills" }]} />}>
              <Route path="/family" element={<FamilyHome />} />
              <Route path="/family/search" element={<FamilySearch />} />
              <Route path="/family/bills" element={<FamilyBills />} />
              <Route path="/family/episodes/:id" element={<EpisodeDesk prefix="/family" />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="Caregiver" />}>
            <Route element={<RoleLayout title="Caregiver" links={[{ to: "/caregiver", label: "Shifts" }, { to: "/caregiver/inbox", label: "Jobs" }]} />}>
              <Route path="/caregiver" element={<CaregiverHome />} />
              <Route path="/caregiver/inbox" element={<InboxPage />} />
              <Route path="/caregiver/episodes/:id" element={<EpisodeDesk prefix="/caregiver" />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="AgencyAdmin" />}>
            <Route element={<RoleLayout title="Agency" links={[{ to: "/agency", label: "Desk" }, { to: "/agency/inbox", label: "Inbox" }]} />}>
              <Route path="/agency" element={<AgencyHome />} />
              <Route path="/agency/inbox" element={<InboxPage />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="HospitalCoordinator" />}>
            <Route element={<RoleLayout title="Hospital" links={[{ to: "/hospital", label: "Discharge" }]} />}>
              <Route path="/hospital" element={<HospitalHome />} />
            </Route>
          </Route>
          <Route element={<RequireRole role="PlatformAdmin" />}>
            <Route element={<RoleLayout title="Admin" links={[{ to: "/admin", label: "KYC" }, { to: "/admin/audit", label: "Audit" }]} />}>
              <Route path="/admin" element={<AdminHome />} />
              <Route path="/admin/audit" element={<AdminAudit />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
