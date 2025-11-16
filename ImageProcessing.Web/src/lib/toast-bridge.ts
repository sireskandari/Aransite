// Simple bridge so non-React code (axios) can show toasts.
// We'll set these functions at runtime from inside React.

let errorImpl = (msg: string) => console.error("[toast.error]", msg);
let infoImpl = (msg: string) => console.info("[toast.info]", msg);
let successImpl = (msg: string) => console.log("[toast.success]", msg);

export function registerToastFns(fns: {
  error: (msg: string) => void;
  info: (msg: string) => void;
  success: (msg: string) => void;
}) {
  errorImpl = fns.error;
  infoImpl = fns.info;
  successImpl = fns.success;
}

export function toastError(msg: string) {
  errorImpl(msg);
}
export function toastInfo(msg: string) {
  infoImpl(msg);
}
export function toastSuccess(msg: string) {
  successImpl(msg);
}
