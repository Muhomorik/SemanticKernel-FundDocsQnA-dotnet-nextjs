import { Snowflake } from "lucide-react";

export function DemoBanner() {
  return (
    <div className="border-border/30 bg-muted/20 border-b px-4 py-2">
      <div className="mx-auto max-w-3xl space-y-1.5">
        <p className="text-muted-foreground text-center text-xs leading-relaxed">
          <span className="font-medium">Demo Notice:</span> This site runs on
          free tier resources and may experience downtime when limits are
          reached.
          <br />
          Currently processing 15 of 68 SEB funds.
        </p>
        <p className="flex items-center justify-center gap-1.5 text-center text-xs leading-relaxed text-sky-600 dark:text-sky-400">
          <Snowflake className="h-3.5 w-3.5 shrink-0" />
          <span>~30s cold start after 1 hour of inactivity.</span>
        </p>
      </div>
    </div>
  );
}
