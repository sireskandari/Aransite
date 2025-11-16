// src/pages/components/HeroHeading.tsx
import { ArrowUpRight } from "lucide-react";
import { Button } from "@/components/ui/button";

interface HeroHeadingProps {
  heading?: string;
  subheading?: string;
  description?: string;
  image?: {
    src: string;
    alt: string;
  };
  buttons?: {
    primary?: {
      text: string;
      url: string;
    };
    secondary?: {
      text: string;
      url: string;
    };
  };
}

const HeroHeading = ({
  heading = "Aran Solution ",
  subheading = "Edge Intelligence for Real-World Applications",
  description = "We design and build edge-first software solutions that turn camera streams and sensor data into real-time insights. From construction sites to smart facilities, Aran Solution helps you monitor, detect, and react — right where the data is created.",
  buttons = {
    primary: {
      text: "Request a Project Quote",
      url: "#contact",
    },
    secondary: {
      text: "Talk to our team",
      url: "tel:+1-000-000-0000",
    },
  },
  image = {
    src: "https://deifkwefumgah.cloudfront.net/shadcnblocks/block/placeholder-dark-7-tall.svg",
    alt: "Edge analytics dashboard",
  },
}: HeroHeadingProps) => {
  return (
    <section className="py-10 lg:py-20">
      <div className="container flex flex-col items-center gap-10 lg:flex-row">
        {/* Text */}
        <div className="flex flex-col gap-7 lg:w-2/3">
          <p className="text-sm font-medium tracking-[0.25em] text-muted-foreground uppercase">
            Software for the Edge
          </p>
          <h1 className="text-foreground text-4xl font-semibold md:text-5xl lg:text-6xl">
            <span>{heading}</span>
            <span className="block text-muted-foreground">{subheading}</span>
          </h1>
          <p className="text-muted-foreground text-base md:text-lg lg:text-xl max-w-2xl">
            {description}
          </p>
          <div className="flex flex-wrap items-start gap-4 lg:gap-6">
            <Button asChild size="lg">
              <a href={buttons.primary?.url}>
                <div className="flex items-center gap-2">
                  <ArrowUpRight className="size-4" />
                  <span className="whitespace-nowrap text-sm lg:text-base">
                    {buttons.primary?.text}
                  </span>
                </div>
              </a>
            </Button>
            <Button asChild variant="ghost" className="gap-2" size="lg">
              <a href={buttons.secondary?.url}>
                <span>{buttons.secondary?.text}</span>
              </a>
            </Button>
          </div>
        </div>

        {/* Mockup image */}
        <div className="relative z-10">
          <div className="absolute left-1/2 top-2.5 h-[92%] w-[69%] -translate-x-[52%] overflow-hidden rounded-[35px] border border-border/50 bg-background/40 backdrop-blur">
            <img
              src={image.src}
              alt={image.alt}
              className="size-full object-cover object-[50%_0%]"
            />
          </div>
          <img
            className="relative z-10"
            src="https://deifkwefumgah.cloudfront.net/shadcnblocks/block/mockups/phone-2.png"
            width={450}
            height={889}
            alt="Mobile edge dashboard"
          />
        </div>
      </div>
    </section>
  );
};

export { HeroHeading };
