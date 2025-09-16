import { Routes, Route } from "react-router-dom";
import AppLayout from "./app-layout";
import HomePage from "./routes/home";
import NotFoundPage from "./routes/not-found";
import RequireAuth from "@/features/auth/components/require-auth";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route
          index
          element={
            <RequireAuth>
              <HomePage />
            </RequireAuth>
          }
        />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
