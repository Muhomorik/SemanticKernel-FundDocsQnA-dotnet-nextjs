interface SankeyEmptyProps {
  variant: "none" | "only-in" | "only-out";
}

export function SankeyEmpty({ variant }: SankeyEmptyProps) {
  const message =
    variant === "none"
      ? "No ownership changes for this period."
      : variant === "only-in"
        ? "Only inflows this period — no funds lost owners."
        : "Only outflows this period — no funds gained owners.";

  return (
    <div className="text-muted-foreground flex items-center justify-center py-12 text-sm">
      {message}
    </div>
  );
}
