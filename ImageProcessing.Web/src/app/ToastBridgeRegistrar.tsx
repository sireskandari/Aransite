import { useEffect } from "react";
import { registerToastFns } from "@/lib/toast-bridge";
import { useToast } from "@/components/ui/Toaster"; // <-- uses your existing Toaster

export default function ToastBridgeRegistrar() {
  const toast = useToast();

  useEffect(() => {
    registerToastFns({
      error: (m) => toast({ type: "error", message: m }),
      info: (m) => toast({ type: "info", message: m }),
      success: (m) => toast({ type: "success", message: m }),
    });
  }, [toast]);

  return null;
}
