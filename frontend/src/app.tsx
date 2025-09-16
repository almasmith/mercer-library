import { Routes, Route } from "react-router-dom";
import AppLayout from "./app-layout";
import HomePage from "./routes/home";
import NotFoundPage from "./routes/not-found";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<AppLayout />}>
        <Route index element={<HomePage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
