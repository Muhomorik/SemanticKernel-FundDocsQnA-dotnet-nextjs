"use client";

import { useTheme } from "next-themes";
import {
  FAMILIES,
  familyColour,
  symbolFor,
  verdictColour,
  type FamilyId,
} from "@/lib/fund-clusters";

const SHAPES = {
  circle: "rounded-full",
  diamond: "rotate-45 scale-[0.85] rounded-[1px]",
  rect: "rounded-[1px]",
};

export function ClusterLegend() {
  const dark = useTheme().resolvedTheme === "dark";
  const families = Object.entries(FAMILIES) as [
    FamilyId,
    (typeof FAMILIES)[FamilyId],
  ][];

  return (
    <ul className="text-muted-foreground border-border/60 flex flex-wrap gap-x-4 gap-y-1.5 border-t px-3 py-2 text-xs">
      {families.map(([id, family]) => (
        <li key={id} className="flex items-center gap-1.5">
          <span
            aria-hidden="true"
            className={`inline-block h-2.5 w-2.5 ${SHAPES[symbolFor(id)]}`}
            style={{ background: familyColour(id, 1, dark) }}
          />
          {family.name}
        </li>
      ))}
      {(
        [
          ["neighbouring", "Neighbouring category"],
          ["different", "Different family"],
        ] as const
      ).map(([verdict, label]) => (
        <li key={verdict} className="flex items-center gap-1.5">
          <span
            aria-hidden="true"
            className="inline-block h-2.5 w-2.5 rounded-full border-2"
            style={{ borderColor: verdictColour(verdict, dark) ?? undefined }}
          />
          {label}
        </li>
      ))}
    </ul>
  );
}
