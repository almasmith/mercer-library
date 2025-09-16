import { ReactNode, useState } from "react";

export function useConfirm() {
  const [state, setState] = useState<{ open: boolean; message: string; resolve?: (ok: boolean) => void }>({ open: false, message: "" });

  const Confirm = () =>
    state.open ? (
      <div role="dialog" aria-modal="true" className="fixed inset-0 z-50 grid place-items-center bg-black/30">
        <div className="w-full max-w-sm rounded bg-white p-4 shadow">
          <p className="text-sm">{state.message}</p>
          <div className="mt-4 flex justify-end gap-2">
            <button className="rounded border px-3 py-1.5" onClick={() => { state.resolve?.(false); setState(s => ({ ...s, open: false })); }}>Cancel</button>
            <button className="rounded bg-red-600 px-3 py-1.5 text-white" onClick={() => { state.resolve?.(true); setState(s => ({ ...s, open: false })); }}>Delete</button>
          </div>
        </div>
      </div>
    ) : null;

  const confirm = (message: string) =>
    new Promise<boolean>((resolve) => setState({ open: true, message, resolve }));

  return { Confirm, confirm };
}


