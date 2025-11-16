import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuthStore } from "@/features/auth/authStore";

export default function LogoutPage() {
  const navigate = useNavigate();
  const logout = useAuthStore((s) => s.logout);

  useEffect(() => {
    (async () => {
      try {
        await logout(); // calls backend and clears local user state
      } catch {
        // ignore errors during logout
      } finally {
        navigate("/login", { replace: true });
      }
    })();
  }, [logout, navigate]);

  return null; // or render a spinner while redirecting
}
