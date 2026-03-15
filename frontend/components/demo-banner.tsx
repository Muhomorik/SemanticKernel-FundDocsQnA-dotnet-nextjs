import { Snowflake } from "lucide-react";

export function DemoBanner() {
  return (
    <div className="border-warm/15 bg-warm/[0.03] dark:bg-warm/[0.06] border-b px-4 py-2.5">
      <div className="mx-auto max-w-3xl space-y-1.5">
        <p className="text-muted-foreground text-center text-xs leading-relaxed">
          <span className="text-warm mr-1 font-serif text-[11px] font-semibold tracking-wide uppercase italic">
            Demo
          </span>
          <span className="text-border mx-1" aria-hidden>
            |
          </span>
          Free tier resources &mdash; may experience downtime when limits are
          reached.
          <br />
          Currently processing 15 of 68 SEB funds.
        </p>
        <p className="flex items-center justify-center gap-1.5 text-center text-[11px] leading-relaxed text-sky-600 dark:text-sky-400">
          <Snowflake className="h-3 w-3 shrink-0" />
          <span>~30s cold start after 1 hour of inactivity.</span>
        </p>
      </div>
    </div>
  );
}
