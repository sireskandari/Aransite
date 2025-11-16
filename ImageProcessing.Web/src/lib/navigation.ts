// Non-React navigation bridge (so axios can redirect after 401)
let navImpl = (path: string) => {
  // Fallback if router not registered yet
  if (window.location.pathname !== path) window.location.href = path;
};

export function registerNavigator(fn: (path: string) => void) {
  navImpl = fn;
}
export function navigateToLogin() {
  navImpl("/login");
}
