import { formatShare, MethodFile } from "@/lib/fund-clusters";

interface Stat {
  value: string;
  label: string;
  tone?: string;
}

export function ClusterStats({ doc }: { doc: MethodFile }) {
  const s = doc.summary;
  const stats: Stat[] = [
    { value: formatShare(s.agree), label: "Agree", tone: "text-foreground" },
    {
      value: formatShare(s.neighbouring),
      label: "Neighbouring",
      tone: "text-amber-600 dark:text-amber-400",
    },
    {
      value: formatShare(s.different),
      label: "Different family",
      tone: "text-destructive",
    },
    { value: String(s.notInCluster), label: "No cluster" },
    { value: s.ari?.toFixed(2) ?? "–", label: "ARI" },
    { value: s.nmi?.toFixed(2) ?? "–", label: "NMI" },
  ];

  return (
    <ul
      title={doc.description}
      className="flex flex-wrap gap-x-6 gap-y-2 tabular-nums"
    >
      {stats.map(({ value, label, tone }) => (
        <li key={label} className="flex flex-col">
          <span
            className={`font-serif text-xl leading-tight font-medium ${tone ?? "text-muted-foreground"}`}
          >
            {value}
          </span>
          <span className="text-muted-foreground text-xs">{label}</span>
        </li>
      ))}
    </ul>
  );
}
