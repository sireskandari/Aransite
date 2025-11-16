// src/pages/ContactPage.tsx
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";

export default function ContactPage() {
  return (
    <div className="flex min-h-screen flex-col text-foreground">
      <main className="flex-1">
        <section id="contact" className="py-12 md:py-16">
          <div className="container grid gap-10 md:grid-cols-[1.4fr,1fr]">
            <Card>
              <CardHeader>
                <CardTitle>Tell us about your project</CardTitle>
              </CardHeader>
              <CardContent>
                <form
                  className="space-y-4"
                  onSubmit={(e) => e.preventDefault()}
                >
                  <div className="grid gap-4 sm:grid-cols-2">
                    <div className="space-y-1.5">
                      <Label htmlFor="name">Name</Label>
                      <Input id="name" name="name" placeholder="Your name" />
                    </div>
                    <div className="space-y-1.5">
                      <Label htmlFor="email">Email</Label>
                      <Input
                        id="email"
                        name="email"
                        type="email"
                        placeholder="you@example.com"
                      />
                    </div>
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="company">Company (optional)</Label>
                    <Input
                      id="company"
                      name="company"
                      placeholder="Your company"
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="projectType">Project focus</Label>
                    <Input
                      id="projectType"
                      name="projectType"
                      placeholder="e.g. construction site monitoring, facility analytics..."
                    />
                  </div>

                  <div className="space-y-1.5">
                    <Label htmlFor="message">Project details</Label>
                    <Textarea
                      id="message"
                      name="message"
                      rows={5}
                      placeholder="Share timelines, goals, existing hardware, or anything else that helps us understand your needs."
                    />
                  </div>

                  <Button type="submit" size="lg" className="w-full sm:w-auto">
                    Send message
                  </Button>
                </form>
              </CardContent>
            </Card>

            <div className="space-y-6 text-sm text-muted-foreground">
              <div>
                <h2 className="text-base font-semibold text-foreground mb-2">
                  Prefer to talk?
                </h2>
                <p>
                  You can also reach us directly for a quick call about your
                  project, hardware setup, or timelines.
                </p>
                <p className="mt-3">
                  <span className="font-medium text-foreground">Phone:</span>{" "}
                  <a href="tel:+1-437-484-5474" className="hover:text-primary">
                    +1 (437) 484-5474
                  </a>
                  <br />
                  <span className="font-medium text-foreground">
                    Email:
                  </span>{" "}
                  <a
                    href="mailto:info@aransolution.com"
                    className="hover:text-primary"
                  >
                    info@aransolution.com
                  </a>
                </p>
              </div>

              <div className="rounded-2xl border bg-card p-4">
                <h3 className="text-sm font-semibold text-foreground mb-1">
                  Typical next steps
                </h3>
                <ol className="list-decimal pl-4 space-y-1.5">
                  <li>Short intro call (15–30 minutes).</li>
                  <li>Rough scope & ballpark estimate.</li>
                  <li>Technical deep dive and final proposal.</li>
                </ol>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  );
}
