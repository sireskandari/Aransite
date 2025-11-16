import { createBrowserRouter } from "react-router-dom";
import MainLayout from "../app/layout/MainLayout";
import AuthLayout from "../app/layout/AuthLayout";
import HomePage from "../pages/HomePage";
import AboutPage from "../pages/AboutPage";
import ContactPage from "../pages/ContactPage";
import NotFoundPage from "../pages/NotFoundPage";
import LoginPage from "../features/auth/LoginPage";
import LogoutPage from "../features/auth/LogoutPage";
import UsersListPage from "../features/users/UsersListPage";
import CamerasListPage from "../features/cameras/CamerasListPage";
import ProtectedRoute from "../features/auth/ProtectedRoute";
import DetectTargetsListPage from "../features/detectTargets/DetectTargetsListPage";
import VisionListPage from "../features/vision/VisionListPage";

export const router = createBrowserRouter([
  {
    element: <MainLayout />,
    children: [
      { path: "/", element: <HomePage /> },
      { path: "/contact-us", element: <ContactPage /> },
      { path: "/about-us", element: <AboutPage /> },

      {
        path: "/vision",
        element: (
          <ProtectedRoute roles={["Admin", "User"]}>
            <VisionListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: "/vision",
        element: (
          <ProtectedRoute roles={["Admin", "User"]}>
            <VisionListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: "/targets",
        element: (
          <ProtectedRoute roles={["Admin", "User"]}>
            <DetectTargetsListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: "/cameras",
        element: (
          <ProtectedRoute roles={["Admin", "User"]}>
            <CamerasListPage />
          </ProtectedRoute>
        ),
      },
      {
        path: "/users",
        element: (
          <ProtectedRoute roles={["Admin", "User"]}>
            <UsersListPage />
          </ProtectedRoute>
        ),
      },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
  {
    element: <AuthLayout />,
    children: [
      { path: "/login", element: <LoginPage /> },
      { path: "/logout", element: <LogoutPage /> },
    ],
  },
]);
