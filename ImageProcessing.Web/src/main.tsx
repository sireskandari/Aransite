import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router-dom";
import QueryProvider from "./app/providers/QueryProvider";
import { router } from "./app/routes";
import "./styles/index.css";
import { ToasterProvider } from "./components/ui/Toaster";
import ToastBridgeRegistrar from "./app/ToastBridgeRegistrar";
createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryProvider>
      <ToasterProvider>
        <ToastBridgeRegistrar />
        <RouterProvider router={router} />
      </ToasterProvider>
    </QueryProvider>
  </StrictMode>
);
