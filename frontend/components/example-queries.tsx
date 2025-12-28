"use client";

import * as React from "react";
import { cn } from "@/lib/utils";

const QUERY_CATEGORIES = [
  {
    title: "Fund Basics",
    queries: [
      "What is this fund and what does it invest in?",
      "Why is Japan excluded from this Asian fund?",
      'What does "Article 8" classification mean?',
      "What is the benchmark index?",
    ],
  },
  {
    title: "Risk & Returns",
    queries: [
      "What is the fund's risk level?",
      "What are the potential returns in different scenarios?",
      "What's the worst-case scenario for my investment?",
      "Is there any capital guarantee?",
      "What currency risk should I be aware of?",
    ],
  },
  {
    title: "Costs",
    queries: [
      "What are the total annual costs?",
      "Are there any entry or exit fees?",
      "What are transaction costs?",
      "How do costs affect my returns over time?",
    ],
  },
  {
    title: "Investing",
    queries: [
      "How long should I hold this investment?",
      "Can I sell my shares at any time?",
      "What's the minimum investment?",
      "Who is this fund suitable for?",
    ],
  },
  {
    title: "Practical",
    queries: [
      "How do I buy or sell shares?",
      "Where can I find more information?",
      "How do I file a complaint?",
      "What happens if SEB Funds AB goes bankrupt?",
      "Does this share class pay dividends?",
    ],
  },
  {
    title: "Comparison",
    queries: [
      "How does this compare to other Asian funds?",
      "Is this fund actively or passively managed?",
    ],
  },
];

interface ExampleQueriesProps {
  onSelect: (query: string) => void;
  disabled?: boolean;
}

export function ExampleQueries({ onSelect, disabled }: ExampleQueriesProps) {
  const [activeCategory, setActiveCategory] = React.useState<string | null>(
    null
  );

  return (
    <div className="w-full space-y-3">
      {/* Category tabs */}
      <div className="flex flex-wrap justify-center gap-1.5">
        {QUERY_CATEGORIES.map((category, index) => (
          <button
            key={category.title}
            onClick={() =>
              setActiveCategory(
                activeCategory === category.title ? null : category.title
              )
            }
            disabled={disabled}
            className={cn(
              "animate-fade-up rounded-full px-3 py-1.5 text-xs font-medium transition-all duration-200",
              "hover:border-border/80 border",
              activeCategory === category.title
                ? "border-primary/30 bg-primary/10 text-primary"
                : "border-border/40 bg-card text-muted-foreground hover:text-foreground",
              disabled && "pointer-events-none opacity-50"
            )}
            style={{ animationDelay: `${index * 50}ms` }}
          >
            {category.title}
          </button>
        ))}
      </div>

      {/* Query suggestions */}
      <div className="space-y-2">
        {QUERY_CATEGORIES.map((category) => (
          <div
            key={category.title}
            className={cn(
              "grid gap-2 overflow-hidden transition-all duration-300",
              activeCategory === null || activeCategory === category.title
                ? "grid-rows-[1fr] opacity-100"
                : "grid-rows-[0fr] opacity-0"
            )}
          >
            <div className="overflow-hidden">
              {(activeCategory === null ||
                activeCategory === category.title) && (
                <div className="flex flex-wrap justify-center gap-2">
                  {category.queries
                    .slice(0, activeCategory === category.title ? undefined : 2)
                    .map((query, queryIndex) => (
                      <button
                        key={query}
                        onClick={() => onSelect(query)}
                        disabled={disabled}
                        className={cn(
                          "animate-scale-in text-left",
                          "bg-secondary/40 hover:bg-secondary/70 border-border/30 hover:border-border/50 text-foreground/80 hover:text-foreground",
                          "rounded-xl border px-3 py-2 text-sm transition-all duration-200",
                          "max-w-xs truncate",
                          disabled && "pointer-events-none opacity-50"
                        )}
                        style={{
                          animationDelay: `${queryIndex * 30}ms`,
                        }}
                      >
                        {query}
                      </button>
                    ))}
                </div>
              )}
            </div>
          </div>
        ))}
      </div>

      {/* Hint text */}
      <p className="text-muted-foreground/50 text-center text-xs">
        Click a category to see more questions, or click any question to ask it
      </p>
    </div>
  );
}
