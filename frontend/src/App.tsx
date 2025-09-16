import { useState } from "react";

function App() {
  const [count, setCount] = useState(0);

  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-6 bg-slate-950 px-4 text-slate-100">
      <header className="text-center">
        <p className="text-sm uppercase tracking-[0.35em] text-slate-400">Welcome</p>
        <h1 className="text-5xl font-semibold">Vite + React + Tailwind</h1>
      </header>

      <section className="flex flex-col items-center gap-4 rounded-2xl bg-slate-900/60 p-8 shadow-lg shadow-slate-900/50">
        <p className="text-lg text-slate-300">The counter below uses Tailwind utility classes.</p>
        <button
          className="rounded-full bg-indigo-500 px-6 py-3 text-lg font-medium transition hover:bg-indigo-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-400"
          onClick={() => setCount((value) => value + 1)}
        >
          Count: {count}
        </button>
      </section>

      <p className="text-sm text-slate-500">
        Edit <code className="rounded bg-slate-800 px-2 py-1">src/App.tsx</code> and save to test
        HMR.
      </p>
    </main>
  );
}

export default App;
