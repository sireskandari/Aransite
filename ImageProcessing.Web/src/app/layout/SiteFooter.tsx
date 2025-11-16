export function SiteFooter() {
  return (
    <footer className="border-t bg-background">
      <div className="flex flex-col items-center justify-center text-center gap-3 py-6 text-xs text-muted-foreground">
        <p>© {new Date().getFullYear()} Aran Solution. All rights reserved.</p>
        <p className="flex flex-wrap justify-center gap-3">
          <span>Edge applications & real-time vision.</span>
          <span>Contact: info@aransolution.com</span>
        </p>
      </div>
    </footer>
  );
}
