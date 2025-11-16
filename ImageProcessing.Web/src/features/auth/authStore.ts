import { create } from "zustand";
import { persist } from "zustand/middleware";
import api from "@/lib/axios";

type Tokens = {
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiresUtc?: string | null;
  refreshTokenExpiresUtc?: string | null;
};

type User = {
  sub?: string;
  email?: string;
  role?: string;
  name?: string;
} | null;

type AuthState = Tokens & {
  user: User;
  login: (email: string, password: string) => Promise<void>;
  refresh: () => Promise<void>;
  logout: () => Promise<void>;
  loadMe: () => Promise<void>;
};

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      user: null,

      async login(email, password) {
        const { data } = await api.post("/auth/login", { email, password });
        set({
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          accessTokenExpiresUtc: data.accessTokenExpiresUtc,
          refreshTokenExpiresUtc: data.refreshTokenExpiresUtc,
        });
        await get().loadMe();
      },

      async refresh() {
        const rt = get().refreshToken;
        if (!rt) throw new Error("No refresh token");
        const { data } = await api.post("/auth/refresh", { refreshToken: rt });
        set({
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          accessTokenExpiresUtc: data.accessTokenExpiresUtc,
          refreshTokenExpiresUtc: data.refreshTokenExpiresUtc,
        });
      },

      async loadMe() {
        try {
          const { data } = await api.get("/auth/me");
          set({ user: data });
        } catch {
          set({ user: null });
        }
      },

      async logout() {
        const rt = get().refreshToken;
        if (rt) {
          try {
            await api.post("/auth/logout", { refreshToken: rt });
          } catch {}
        }
        set({
          accessToken: null,
          refreshToken: null,
          accessTokenExpiresUtc: null,
          refreshTokenExpiresUtc: null,
          user: null,
        });
      },
    }),
    { name: "spp-auth" } // localStorage key
  )
);
