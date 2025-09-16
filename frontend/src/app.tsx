import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "./app-layout";
import HomePage from "./routes/home";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<HomePage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
