// src/pages/HomePage.tsx
import { HeroHeading } from "@/pages/components/HeroHeading";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export default function HomePage() {
  return (
    <div className="flex min-h-screen flex-col text-foreground">
      <main className="flex-1">
        <HeroHeading />

        {/* Services / highlights */}
        <section className="border-y bg-muted/30 py-10">
          <div className="container space-y-8">
            <div className="max-w-xl space-y-2">
              <h2 className="text-2xl font-semibold">What we build</h2>
              <p className="text-sm text-muted-foreground">
                Aran Solution focuses on edge-ready, camera-aware systems that
                are reliable in the real world — even with limited bandwidth or
                tough environments.
              </p>
            </div>

            <div className="grid gap-6 md:grid-cols-3">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    Real-Time Object Detection
                  </CardTitle>
                </CardHeader>
                <CardContent className="text-sm text-muted-foreground">
                  We integrate with your existing cameras to detect people,
                  vehicles, and custom objects — sending clean events into your
                  apps and dashboards.
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    Edge-First Architectures
                  </CardTitle>
                </CardHeader>
                <CardContent className="text-sm text-muted-foreground">
                  Processing happens where data is generated, reducing cloud
                  costs and improving latency. Perfect for construction, retail,
                  and smart facilities.
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    Custom Project Delivery
                  </CardTitle>
                </CardHeader>
                <CardContent className="text-sm text-muted-foreground">
                  From proof-of-concept to full deployment, we work with your
                  team to scope, design, and ship solutions that match your
                  timeline and budget.
                </CardContent>
              </Card>
            </div>
          </div>
        </section>

        {/* CTA to contact */}
        <section className="py-14">
          <div className="container flex flex-col items-start justify-between gap-6 rounded-2xl border bg-card p-6 md:flex-row md:items-center md:p-10">
            <div className="space-y-2">
              <h3 className="text-xl font-semibold">Have a project in mind?</h3>
              <p className="text-sm text-muted-foreground max-w-xl">
                Share a bit about your use case and we’ll follow up with a rough
                timeline and ballpark estimate.
              </p>
            </div>
            <Button asChild size="lg">
              <a href="#contact">Request a quote</a>
            </Button>
          </div>
        </section>
      </main>
    </div>
  );
}
