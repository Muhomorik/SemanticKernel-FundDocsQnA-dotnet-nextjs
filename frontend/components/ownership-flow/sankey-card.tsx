import { FlowSide, TooltipState } from "@/lib/ownership-flow";
import { SankeyChart } from "./sankey-chart";
import { SankeyEmpty } from "./sankey-empty";

interface SankeyCardProps {
  title: string;
  subtitle: string;
  data: FlowSide | undefined;
  svgHeight: number;
  isLoading: boolean;
  periodLabel?: string;
  onTooltipChange: (updater: TooltipState | ((prev: TooltipState) => TooltipState)) => void;
}

export function SankeyCard({
  title,
  subtitle,
  data,
  svgHeight,
  isLoading,
  periodLabel,
  onTooltipChange,
}: SankeyCardProps) {
  return (
    <div className="bg-card border-border/50 rounded-xl border p-6">
      {/* Card header */}
      <div className="mb-1 flex flex-wrap items-center gap-2">
        <h2 className="font-serif text-lg font-medium">{title}</h2>
        {periodLabel && (
          <span className="bg-muted text-muted-foreground rounded-md px-2 py-0.5 font-serif text-xs italic">
            {periodLabel}
          </span>
        )}
        {data && (
          <>
            <span className="text-destructive bg-destructive/10 rounded-md px-2 py-0.5 text-xs">
              {data.out.length} outflow{data.out.length !== 1 ? "s" : ""}
            </span>
            <span className="rounded-md bg-emerald-500/10 px-2 py-0.5 text-xs text-emerald-600 dark:text-emerald-400">
              {data.in.length} inflow{data.in.length !== 1 ? "s" : ""}
            </span>
          </>
        )}
      </div>
      <p className="text-muted-foreground mb-4 text-sm">{subtitle}</p>

      {/* Chart area */}
      {isLoading ? (
        <div
          className="bg-muted animate-pulse rounded-lg"
          style={{ height: svgHeight }}
          aria-label="Loading chart"
        />
      ) : data ? (
        <SankeyChart
          data={data}
          svgHeight={svgHeight}
          onTooltipChange={onTooltipChange}
        />
      ) : (
        <SankeyEmpty variant="none" />
      )}
    </div>
  );
}
