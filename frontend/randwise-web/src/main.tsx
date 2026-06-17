import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { AuthProvider } from "./auth/AuthContext";
import { router } from "./router";
import "./styles.css";
import { ThemeModeProvider } from "./theme/ThemeModeContext";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThemeModeProvider>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </ThemeModeProvider>
  </StrictMode>
);
