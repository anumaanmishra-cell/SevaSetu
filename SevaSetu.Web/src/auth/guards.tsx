import { Navigate, Outlet } from "react-router-dom";
import { homeFor, isValidRole, useAuth } from "./AuthContext";
import type { Role } from "../api/types";

export function RequireRole({ role }: { role: Role | Role[] }) {
  const { role: current } = useAuth();
  if (!isValidRole(current)) return <Navigate to="/login" replace />;
  const allowed = Array.isArray(role) ? role : [role];
  if (!allowed.includes(current)) return <Navigate to="/login" replace />;
  return <Outlet />;
}

export function GuestOnly() {
  const { role } = useAuth();
  if (isValidRole(role)) return <Navigate to={homeFor(role)} replace />;
  return <Outlet />;
}
