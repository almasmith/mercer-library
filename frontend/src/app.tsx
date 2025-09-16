import { Routes, Route } from "react-router-dom";
import AppLayout from "./app-layout";
import BooksListPage from "./routes/books/list";
import NewBookPage from "./routes/books/new";
import EditBookPage from "./routes/books/edit";
import NotFoundPage from "./routes/not-found";
import RequireAuth from "@/features/auth/components/require-auth";
import LoginPage from "./routes/auth/login";
import RegisterPage from "./routes/auth/register";
import StatsPage from "./routes/stats";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route
          index
          element={
            <RequireAuth>
              <BooksListPage />
            </RequireAuth>
          }
        />
        <Route path="books">
          <Route
            path="new"
            element={
              <RequireAuth>
                <NewBookPage />
              </RequireAuth>
            }
          />
          <Route
            path=":id/edit"
            element={
              <RequireAuth>
                <EditBookPage />
              </RequireAuth>
            }
          />
        </Route>
        <Route
          path="/stats"
          element={
            <RequireAuth>
              <StatsPage />
            </RequireAuth>
          }
        />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
