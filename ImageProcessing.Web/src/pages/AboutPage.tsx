export default function AboutPage() {
  return (
    <div className="flex min-h-screen flex-col text-foreground">
      <main className="flex-1">
        <section className="py-12 md:py-16">
          <div className="container grid gap-10 md:grid-cols-[2fr,1.5fr]">
            <div className="space-y-6">
              <h1 className="text-3xl font-semibold md:text-4xl">
                About Aran Solution
              </h1>
              <p className="text-sm text-muted-foreground">
                Aran Solution is a small, focused software studio that builds
                edge-ready, camera-aware systems. We bridge the gap between
                classical software development and real-world hardware: cameras,
                sensors, gateways, and industrial environments.
              </p>
              <p className="text-sm text-muted-foreground">
                Our work usually starts with a real problem: monitoring a
                construction site, tracking activity around sensitive zones, or
                automating manual reporting. From there, we design lightweight,
                maintainable solutions that plug into your existing stack.
              </p>
              <div>
                <h2 className="text-base font-semibold mb-2">
                  What matters to us
                </h2>
                <ul className="space-y-2 text-sm text-muted-foreground">
                  <li>
                    • Simple, reliable architectures that your team can own.
                  </li>
                  <li>• Clear communication and transparent scoping.</li>
                  <li>• Practical AI — only where it actually adds value.</li>
                </ul>
              </div>
            </div>

            <div className="space-y-4 rounded-2xl border bg-card p-5 text-sm text-muted-foreground">
              <h3 className="text-base font-semibold text-foreground">
                Quick facts
              </h3>
              <ul className="space-y-2">
                <li>
                  <span className="font-medium text-foreground">
                    Focus areas:
                  </span>{" "}
                  edge applications, camera vision, real-time detection
                </li>
                <li>
                  <span className="font-medium text-foreground">
                    Typical clients:
                  </span>{" "}
                  construction, facility management, security, industrial
                  operations
                </li>
                <li>
                  <span className="font-medium text-foreground">
                    Engagement types:
                  </span>{" "}
                  pilots, MVPs, integrations, long-term support
                </li>
              </ul>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
