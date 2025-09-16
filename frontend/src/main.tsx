import React from "react";
import ReactDOM from "react-dom/client";
import App from "./app";
import "./index.css";
import { AppProviders } from "./providers/app-providers";
import { env } from "./lib/env";


void env; // ensure validation runs on startup
ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AppProviders>
      <App />
    </AppProviders>
  </React.StrictMode>
);
